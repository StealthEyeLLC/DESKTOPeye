using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using DESKTOPeye.Protocol;
using DESKTOPeye.World;

namespace DESKTOPeye.Kernel;

internal sealed partial class KernelService
{
    readonly WorldStore _store;
    readonly PipeRpcClient _session;
    readonly string _pipe,_runtime;
    readonly ConcurrentDictionary<string,Candidate> _candidates=new(StringComparer.Ordinal);
    readonly ConcurrentQueue<string> _candidateOrder=new();
    readonly ConcurrentDictionary<string,VisualDescriptor> _visual=new(StringComparer.Ordinal);
    readonly ConcurrentQueue<string> _visualOrder=new();
    readonly ConcurrentDictionary<string,(VisualFrameRef Frame,string WindowId,long NativeGeneration,long DesktopEpoch)> _frames=new(StringComparer.Ordinal);
    readonly ConcurrentQueue<string> _frameOrder=new();
    readonly ConcurrentDictionary<string,long> _eventSubscriptions=new(StringComparer.Ordinal);
    readonly SemaphoreSlim _mutation=new(1,1);
    long _kernelEpoch,_sessionHostEpoch,_sessionDesktopEpoch,_desktopEpoch,_displayEpoch;
    volatile bool _coldGap;
    public long KernelEpoch=>_kernelEpoch;

    public KernelService(WorldStore store,PipeRpcClient session,string pipe,string runtime){_store=store;_session=session;_pipe=pipe;_runtime=runtime;}

    public async Task InitializeAsync()
    {
        _kernelEpoch=_store.AdvanceEpoch("kernel_epoch");
        var hello=await SessionCall("hello",new{},5000);
        _sessionHostEpoch=GetLong(hello,"hostEpoch");
        _sessionDesktopEpoch=GetLong(hello,"desktopEpoch",1);
        _displayEpoch=GetLong(hello,"displayEpoch",1);
        var previousHost=_store.GetLong("session_host_epoch");
        var previousSessionDesktop=_store.GetLong("session_desktop_epoch");
        _desktopEpoch=_store.GetLong("desktop_epoch",1);
        if(previousHost!=0)
        {
            if(previousHost!=_sessionHostEpoch||previousSessionDesktop==0||previousSessionDesktop!=_sessionDesktopEpoch)
            {
                _coldGap=true; _desktopEpoch=_store.AdvanceEpoch("desktop_epoch");
                InvalidateDesktopBindings(previousHost!=_sessionHostEpoch?"session_host_and_uia_observers_restarted":"interactive_desktop_epoch_changed_while_kernel_absent");
            }
            else
            {
                _store.MarkGap("kernel_restart_session_host_survived",new[]{"kernel","client"});
            }
        }
        _store.SetMeta("session_host_epoch",_sessionHostEpoch.ToString());
        _store.SetMeta("session_desktop_epoch",_sessionDesktopEpoch.ToString());
        _store.SetMeta("display_epoch",_displayEpoch.ToString());
        await RecoverRetainedAsync();
        await ReconstructFocusAsync();
    }

    void InvalidateDesktopBindings(string reason)
    {
        _candidates.Clear();_candidateOrder.Clear();_visual.Clear();_visualOrder.Clear();_frames.Clear();_frameOrder.Clear();_eventSubscriptions.Clear();
        _store.MarkGap(reason,new[]{"session","native","uia","retained"});
        foreach(var c in _store.ListConcepts(includeRetired:false).Where(x=>x.Kind is ConceptKind.Window or ConceptKind.Dialog or ConceptKind.Control or ConceptKind.Collection or ConceptKind.Item))
        {
            foreach(var b in _store.GetBindings(c.Id))_store.SetBindingAvailable(c.Id,b.Provider,false);
            if(c.Identity is IdentityStatus.exact or IdentityStatus.rebound_exact)
                _store.UpsertConcept(c with{Identity=IdentityStatus.stale,Evidence=c.Evidence with{Note=$"desktop/session observation gap: exact continuity withdrawn ({reason})"}},DeltaKind.BindingChanged,c.Id,new{identity="stale",reason});
        }
    }

    async Task<JsonElement> RefreshSessionState(CancellationToken ct=default)
    {
        var s=await SessionCall("session.current",new{},1800,ct);
        var hostDesktop=GetLong(s,"desktopEpoch",_sessionDesktopEpoch);
        if(_sessionDesktopEpoch!=0&&hostDesktop!=_sessionDesktopEpoch)
        {
            _sessionDesktopEpoch=hostDesktop;_store.SetMeta("session_desktop_epoch",hostDesktop.ToString());_coldGap=true;_desktopEpoch=_store.AdvanceEpoch("desktop_epoch");InvalidateDesktopBindings("interactive_desktop_epoch_changed");
        }
        var de=GetLong(s,"displayEpoch",_displayEpoch);
        if(de!=_displayEpoch){_displayEpoch=de;_store.SetMeta("display_epoch",de.ToString());_store.AppendDelta(DeltaKind.DisplayChanged,"session",null,new{displayEpoch=de});}
        return s;
    }
    public Task RunAsync()
    {
        var pump=ObservationPumpAsync();
        var rpc=PipeRpcServer.RunAsync(_pipe,Dispatch);
        return Task.WhenAll(pump,rpc);
    }

    async Task<RpcResponse> Dispatch(RpcRequest req,CancellationToken ct)
    {
        try
        {
            object result=req.Method switch
            {
                "hello" => Hello(),
                "runtime.status" => await RuntimeStatus(),
                "session.current" => await SessionCurrent(),
                "world.cursor" => new{cursor=_store.Head,floor=_store.DeltaFloor,kernelEpoch=_kernelEpoch,desktopEpoch=_desktopEpoch},
                "delta.read" => _store.ReadDeltas(GetOpt<long?>(req.Params,"cursor")??0,GetOpt<int?>(req.Params,"max")??256,GetOpt<string>(req.Params,"scope")),
                "world.sync" => await WorldSync(req.Params,ct),
                "world.get" => GetConceptResult(Get<string>(req.Params,"id")),
                "world.interests" => _store.ListInterests(),
                "app.query" => await AppQuery(req.Params,ct),
                "app.retain" => await RetainCandidate(Get<string>(req.Params,"target"),req.Params,ct),
                "app_instance.current" => await AppInstanceQuery(req.Params,ct),
                "app_instance.retain" => await RetainCandidate(Get<string>(req.Params,"target"),req.Params,ct),
                "window.query" => await WindowQuery(req.Params,ConceptKind.Window,ct),
                "window.retain" => await RetainCandidate(Get<string>(req.Params,"target"),req.Params,ct),
                "window.get_state" => await WindowState(Get<string>(req.Params,"target"),ct),
                "dialog.retain" => await RetainCandidate(Get<string>(req.Params,"target"),req.Params,ct),
                "control.query" => await ControlQuery(req.Params,ConceptKind.Control,ct),
                "control.retain" => await RetainCandidate(Get<string>(req.Params,"target"),req.Params,ct),
                "collection.query" => await ControlQuery(req.Params,ConceptKind.Collection,ct),
                "collection.retain" => await RetainCandidate(Get<string>(req.Params,"target"),req.Params,ct),
                "collection.get_view" => await CollectionView(Get<string>(req.Params,"target"),ct),
                "item.find_by_key" => await ItemFindByKey(req.Params,ct),
                "item.retain" => await RetainCandidate(Get<string>(req.Params,"target"),req.Params,ct),
                "item.resolve" => await ItemResolve(Get<string>(req.Params,"target"),ct),
                "item.get_representation" => await ItemRepresentation(Get<string>(req.Params,"target"),ct),
                "control.invoke" => await SemanticAction(req.Params,"invoke",ct),
                "control.set_value" => await SemanticAction(req.Params,"set_value",ct),
                "control.toggle" => await SemanticAction(req.Params,"toggle",ct),
                "control.expand" => await SemanticAction(req.Params,"expand",ct),
                "control.collapse" => await SemanticAction(req.Params,"collapse",ct),
                "item.realize" => await ItemAction(req.Params,"realize",ct),
                "item.scroll_into_view" => await ItemAction(req.Params,"scroll_into_view",ct),
                "item.select" => await ItemAction(req.Params,"select",ct),
                "collection.sort" => await CollectionCommand(req.Params,"sort",ct),
                "collection.filter" => await CollectionCommand(req.Params,"filter",ct),
                "collection.clear_filter" => await CollectionCommand(req.Params,"clearFilter",ct),
                "capture.window_region" => await CaptureWindowRegion(req.Params,ct),
                "visual.revalidate" => await VisualRevalidate(req.Params,ct),
                "pointer.click" => await PointerClick(req.Params,ct),
                "focus.ensure" => await FocusEnsure(req.Params,ct),
                "keyboard.type" => await KeyboardType(req.Params,ct),
                "wait.selection" => await WaitSelection(req.Params,ct),
                "wait.collection_view_epoch" => await WaitCollectionEpoch(req.Params,ct),
                "wait.control_state" => await WaitControlState(req.Params,ct),
                "wait.value" => await WaitValue(req.Params,ct),
                "wait.dialog_exists" => await WaitDialogExists(req.Params,ct),
                "wait.dialog_closes" => await WaitDialogCloses(req.Params,ct),
                "wait.popup" => await WaitPopup(req.Params,ct),
                "wait.enabled" => await WaitEnabled(req.Params,ct),
                "wait.focus" => await WaitFocus(req.Params,ct),
                "wait.provider_health" => await WaitProviderHealth(req.Params,ct),
                "debug.expire_cursor" => ExpireCursor(req.Params),
                "debug.provider_block" => await SessionCall("debug.worker_block",JsonDefaults.Element(new{affinity=GetOpt<string>(req.Params,"affinity")??"blocked",milliseconds=GetOpt<int?>(req.Params,"milliseconds")??30000,deadlineMs=GetOpt<int?>(req.Params,"providerDeadlineMs")??300}),GetOpt<int?>(req.Params,"deadlineMs")??1500,ct),
                _ => throw new KernelException(ErrorCode.unsupported,$"Unknown method {req.Method}")
            };
            return new RpcResponse(ProtocolVersion.Current,req.Id,true,JsonDefaults.Element(result),null);
        }
        catch(KernelException k){return new RpcResponse(ProtocolVersion.Current,req.Id,false,null,new(k.Code,k.Message,k.NativeCode,k.Detail));}
        catch(RpcCallException r){return new RpcResponse(ProtocolVersion.Current,req.Id,false,null,r.Error);}
        catch(OperationCanceledException){return new RpcResponse(ProtocolVersion.Current,req.Id,false,null,new(ErrorCode.timeout,"deadline expired"));}
        catch(Exception ex){return new RpcResponse(ProtocolVersion.Current,req.Id,false,null,new(ErrorCode.native_error,ex.Message,null,ex.ToString()));}
    }

    object Hello()=>new{version=ProtocolVersion.Current,pid=Environment.ProcessId,kernelEpoch=_kernelEpoch,sessionHostEpoch=_sessionHostEpoch,sessionDesktopEpoch=_sessionDesktopEpoch,desktopEpoch=_desktopEpoch,displayEpoch=_displayEpoch,worldSequence=_store.Head,deltaFloor=_store.DeltaFloor,coldGap=_coldGap,database=_store.Path};
    async Task<object> RuntimeStatus()=>new{kernel=Hello(),sessionHost=await SessionCall("hello",new{},2000),workers=await SessionCall("workers.status",new{},2500),quickCheck=_store.QuickCheck,journalMode=_store.JournalMode,concepts=_store.ListConcepts(includeRetired:false).Count,interests=_store.ListInterests().Count};
    async Task<object> SessionCurrent()
    {
        var s=await RefreshSessionState();
        return new{session=s,kernelEpoch=_kernelEpoch,desktopEpoch=_desktopEpoch,worldSequence=_store.Head};
    }
    async Task<object> AppQuery(JsonElement p,CancellationToken ct)
    {
        var native=await SessionCall<NativeWindowObservation[]>("native.snapshot",new{},2500,ct)??Array.Empty<NativeWindowObservation>();
        var title=GetOpt<string>(p,"windowTitle"); var contains=GetOpt<string>(p,"titleContains"); var image=GetOpt<string>(p,"imagePath"); var processName=GetOpt<string>(p,"processName");
        var list=new List<object>();
        foreach(var g in native.Where(n=>n.Visible).GroupBy(n=>(n.ProcessId,n.ProcessStartFileTime)))
        {
            if(g.Key.ProcessStartFileTime==0)continue; var path=ProcessImagePath(g.Key.ProcessId); var name=Path.GetFileNameWithoutExtension(path);
            if(image!=null&&!string.Equals(path,image,StringComparison.OrdinalIgnoreCase))continue;
            if(processName!=null&&!string.Equals(name,processName,StringComparison.OrdinalIgnoreCase))continue;
            if(title!=null&&!g.Any(x=>x.Title==title))continue; if(contains!=null&&!g.Any(x=>x.Title.Contains(contains,StringComparison.OrdinalIgnoreCase)))continue;
            var appKey=string.IsNullOrEmpty(path)?$"pid-family:{name}":Path.GetFullPath(path).ToLowerInvariant(); var ev=Evidence("application_executable",new(){["imagePath"]=path,["processName"]=name},"query candidate"); var id=CandidateId(); var c=new AppCandidate(id,appKey,name,path,g.Key.ProcessId,g.Key.ProcessStartFileTime,g.First().Title,ev);RememberCandidate(c); list.Add(CandidateView(c));
        }
        return QueryResult(list);
    }

    async Task<object> AppInstanceQuery(JsonElement p,CancellationToken ct)
    {
        var app=RequireConcept(Get<string>(p,"app"),ConceptKind.App); var appPath=GetProp<string>(app,"imagePath")??"";
        foreach(var old in _store.ListConcepts(ConceptKind.AppInstance,includeRetired:false).Where(x=>x.ParentId==app.Id).ToArray())try{await ReconcileAppInstance(old,ct);}catch{}
        var native=await SessionCall<NativeWindowObservation[]>("native.snapshot",new{},2500,ct)??Array.Empty<NativeWindowObservation>(); var groups=native.GroupBy(x=>(x.ProcessId,x.ProcessStartFileTime)).Where(g=>g.Key.ProcessStartFileTime!=0&&string.Equals(ProcessImagePath(g.Key.ProcessId),appPath,StringComparison.OrdinalIgnoreCase)).ToArray(); var l=new List<object>();
        foreach(var g in groups){var ev=Evidence("process_incarnation",new(){["appId"]=app.Id,["processId"]=g.Key.ProcessId.ToString(),["startFileTime"]=g.Key.ProcessStartFileTime.ToString()},"exact process incarnation");var id=CandidateId();var c=new AppInstanceCandidate(id,app.Id,g.Key.ProcessId,g.Key.ProcessStartFileTime,appPath,ev);RememberCandidate(c);l.Add(CandidateView(c));}
        return QueryResult(l);
    }

    async Task<object> WindowQuery(JsonElement p,ConceptKind requestedKind,CancellationToken ct)
    {
        var appinst=RequireConcept(Get<string>(p,"appInstance"),ConceptKind.AppInstance); var pid=(uint)(GetProp<long?>(appinst,"processId")??0); var start=GetProp<long?>(appinst,"processStartFileTime")??0; var title=GetOpt<string>(p,"title"); var contains=GetOpt<string>(p,"titleContains"); var aid=GetOpt<string>(p,"automationId"); var ownerId=GetOpt<string>(p,"owner"); long ownerHwnd=0;if(ownerId!=null)ownerHwnd=NativeBinding(RequireConcept(ownerId)).Hwnd;
        var native=await SessionCall<NativeWindowObservation[]>("native.snapshot",new{},2500,ct)??Array.Empty<NativeWindowObservation>(); var matches=native.Where(n=>n.ProcessId==pid&&n.ProcessStartFileTime==start&&n.Visible&&(title==null||n.Title==title)&&(contains==null||n.Title.Contains(contains,StringComparison.OrdinalIgnoreCase))&&(ownerHwnd==0||n.OwnerHwnd==ownerHwnd)).ToArray(); var candidates=new List<WindowCandidate>();
        foreach(var n in matches)
        {
            UiaElementObservation? u=null; try{u=await SessionCall<UiaElementObservation>("uia.observe_handle",new{hwnd=n.Hwnd,affinity=$"app-{pid}"},2500,ct);}catch{}
            if(aid!=null&&(u==null||u.AutomationId!=aid))continue; var kind=requestedKind==ConceptKind.Dialog||n.OwnerHwnd!=0?ConceptKind.Dialog:ConceptKind.Window; var ev=Evidence("native_current_incarnation",new(){["appinst"]=appinst.Id,["hwnd"]=n.Hwnd.ToString(),["generation"]=n.NativeGeneration.ToString(),["processStartFileTime"]=n.ProcessStartFileTime.ToString()},"current native window incarnation");var id=CandidateId();var c=new WindowCandidate(id,kind,appinst.Id,ownerId,n,u,true,ev);RememberCandidate(c);candidates.Add(c);
        }
        var unique=candidates.Count==1; if(!unique)foreach(var c in candidates)_candidates[c.Id]=c with{Unique=false}; return QueryResult(candidates.Select(c=>CandidateView(unique?c:c with{Unique=false})).Cast<object>().ToList());
    }

    async Task<object> ControlQuery(JsonElement p,ConceptKind kind,CancellationToken ct)
    {
        var parentId=Get<string>(p,"parent"); var parent=RequireConcept(parentId); if(parent.Kind is not (ConceptKind.Window or ConceptKind.Dialog))throw new KernelException(ErrorCode.unsupported,"control parent must be a retained window/dialog"); var nb=NativeBinding(parent); EnsureMutableIdentity(parent,false); var pid=(int)(GetProp<long?>(RequireConcept(parent.AppInstanceId!),"processId")??0); var affinity=$"app-{pid}";
        var query=new{rootHwnd=nb.Hwnd,affinity,name=GetOpt<string>(p,"name"),automationId=GetOpt<string>(p,"automationId"),itemStatus=GetOpt<string>(p,"itemStatus"),controlType=GetOpt<int?>(p,"controlType"),limit=GetOpt<int?>(p,"limit")??256}; var arr=await SessionCall<UiaElementObservation[]>("uia.query",query,3000,ct)??Array.Empty<UiaElementObservation>(); if(GetOpt<bool?>(p,"visibleOnly")==true)arr=arr.Where(x=>!x.Offscreen&&x.Bounds.Width>0&&x.Bounds.Height>0).ToArray(); var list=new List<UiaCandidate>(); bool unique=arr.Length==1;
        foreach(var u in arr){var ev=Evidence("same_scope_current_provider_candidate",new(){["parent"]=parent.Id,["providerEpoch"]=u.ProviderEpoch.ToString(),["runtimeId"]=u.RuntimeId,["automationId"]=u.AutomationId},unique?"unique current query candidate":"multiple current candidates");var id=CandidateId();var c=new UiaCandidate(id,kind,parent.Id,parent.AppInstanceId!,nb.Hwnd,u,unique,affinity,ev);RememberCandidate(c);list.Add(c);} return QueryResult(list.Select(CandidateView).Cast<object>().ToList());
    }

    async Task<object> ItemFindByKey(JsonElement p,CancellationToken ct)
    {
        var collection=RequireConcept(Get<string>(p,"collection"),ConceptKind.Collection); EnsureMutableIdentity(collection,false); var key=Get<string>(p,"key"); var ub=UiaBinding(collection); var obs=UiaObservation(ub); var root=GetWitnessLong(ub,"rootHwnd"); var affinity=GetWitnessString(ub,"affinity");
        try
        {
            var u=await SessionCall<UiaElementObservation>("uia.find_item_by_property",new{rootHwnd=root,affinity,collectionLocator=new{runtimeId=obs.RuntimeId},propertyId=30005,value=key},3000,ct)??throw new KernelException(ErrorCode.not_found,"item not found");
            var ev=Evidence("documented_stable_item_key",new(){["collection"]=collection.Id,["key"]=key,["providerEpoch"]=u.ProviderEpoch.ToString()},"fixture exposes Name as documented durable key"); var id=CandidateId(); var c=new ItemCandidate(id,collection.Id,collection.AppInstanceId!,root,u,key,affinity,ev);RememberCandidate(c);return new{count=1,ambiguous=false,candidates=new[]{CandidateView(c)}};
        }
        catch(RpcCallException r) when(r.Error.Code==ErrorCode.not_found){return new{count=0,ambiguous=false,virtualized=true,key,candidates=Array.Empty<object>()};}
    }

    async Task<object> RetainCandidate(string target,JsonElement p,CancellationToken ct)
    {
        if(target.StartsWith("app_",StringComparison.Ordinal)||target.StartsWith("appinst_",StringComparison.Ordinal)||target.StartsWith("window_",StringComparison.Ordinal)||target.StartsWith("dialog_",StringComparison.Ordinal)||target.StartsWith("control_",StringComparison.Ordinal)||target.StartsWith("collection_",StringComparison.Ordinal)||target.StartsWith("item_",StringComparison.Ordinal))return GetConceptResult(target);
        if(!_candidates.TryGetValue(target,out var c))throw new KernelException(ErrorCode.stale,"ephemeral query candidate expired or unknown"); if(c.Identity is IdentityStatus.ambiguous or IdentityStatus.candidate)throw new KernelException(ErrorCode.ambiguous,"candidate is not uniquely promotable");
        await _mutation.WaitAsync(ct); try{return c switch{AppCandidate a=>RetainApp(a),AppInstanceCandidate a=>RetainAppInstance(a),WindowCandidate w=>await RetainWindow(w,p,ct),UiaCandidate u=>await RetainUia(u,p,ct),ItemCandidate i=>await RetainItem(i,ct),_=>throw new KernelException(ErrorCode.unsupported,"candidate kind")};} finally{_mutation.Release();}
    }

    object RetainApp(AppCandidate a)
    {
        var existing=_store.ListConcepts(ConceptKind.App,includeRetired:false,stableKey:a.AppKey).FirstOrDefault(); if(existing!=null)return GetConceptResult(existing.Id); var id=LogicalIds.New(ConceptKind.App);var props=JsonDefaults.Element(new{name=a.Name,imagePath=a.ImagePath,appKey=a.AppKey});var c=new LogicalConcept(id,ConceptKind.App,IdentityStatus.exact,UiState.None,null,null,a.AppKey,0,0,null,props,a.Evidence);c=_store.UpsertConcept(c,DeltaKind.ConceptCreated,id);_store.UpsertInterest(id,"retained",new{application=true});return GetConceptResult(c.Id);
    }
    object RetainAppInstance(AppInstanceCandidate a)
    {
        var key=$"{a.AppId}|{a.ProcessStartFileTime}";var existing=_store.ListConcepts(ConceptKind.AppInstance,includeRetired:false,stableKey:key).FirstOrDefault();if(existing!=null)return GetConceptResult(existing.Id);var id=LogicalIds.New(ConceptKind.AppInstance);var props=JsonDefaults.Element(new{processId=(long)a.ProcessId,processStartFileTime=a.ProcessStartFileTime,imagePath=a.ImagePath});var c=new LogicalConcept(id,ConceptKind.AppInstance,IdentityStatus.exact,UiState.None,a.AppId,null,key,0,0,null,props,a.Evidence);c=_store.UpsertConcept(c,DeltaKind.ConceptCreated,id);_store.UpsertRelation(a.AppId,"instances",id);_store.UpsertInterest(id,"retained",new{processIncarnation=true});return GetConceptResult(c.Id);
    }
    async Task<object> RetainWindow(WindowCandidate w,JsonElement p,CancellationToken ct)
    {
        string? stable=null; var keyProp=GetOpt<string>(p,"documentedKeyProperty"); if(keyProp=="automationId"&&!string.IsNullOrWhiteSpace(w.Uia?.AutomationId))stable=$"uia:automationId:{w.Uia.AutomationId}"; else if(keyProp=="itemStatus"&&!string.IsNullOrWhiteSpace(w.Uia?.ItemStatus))stable=$"uia:itemStatus:{w.Uia.ItemStatus}";
        var existing=FindExistingWindow(w,stable); if(existing!=null){await RebindWindow(existing,w,IdentityStatus.exact,ct);return GetConceptResult(existing.Id);}
        var id=LogicalIds.New(w.WindowKind);var state=WindowUiState(w.Native,w.Uia);var props=JsonDefaults.Element(new{native=w.Native,uia=w.Uia,affinity=$"app-{w.Native.ProcessId}",documentedKeyProperty=keyProp});var c=new LogicalConcept(id,w.WindowKind,IdentityStatus.exact,state,w.ParentId,w.AppInstance,stable,0,0,null,props,w.Evidence);c=_store.UpsertConcept(c,DeltaKind.ConceptCreated,id);_store.UpsertBinding(id,"native",_sessionHostEpoch,id,w.Native); if(w.Uia!=null)_store.UpsertBinding(id,"uia",w.Uia.ProviderEpoch,id,new{rootHwnd=w.Native.Hwnd,affinity=$"app-{w.Native.ProcessId}",observation=w.Uia});_store.UpsertRelation(w.AppInstance,"windows",id);if(w.ParentId!=null)_store.UpsertRelation(w.ParentId,"owns",id);_store.UpsertInterest(id,"retained",new{native=true,uia=true});await EnsureSubscription(c,w.Native.Hwnd,w.Native.ProcessId,ct);return GetConceptResult(c.Id);
    }
    async Task<object> RetainUia(UiaCandidate u,JsonElement p,CancellationToken ct)
    {
        string? stable=null;var kp=GetOpt<string>(p,"documentedKeyProperty");if(kp=="automationId"&&!string.IsNullOrWhiteSpace(u.Uia.AutomationId))stable=$"uia:automationId:{u.Uia.AutomationId}";else if(kp=="itemStatus"&&!string.IsNullOrWhiteSpace(u.Uia.ItemStatus))stable=$"uia:itemStatus:{u.Uia.ItemStatus}";
        var existing=FindExistingUia(u,stable);if(existing!=null){await RebindUia(existing,u.Uia,u.RootHwnd,u.Affinity,IdentityStatus.exact,ct);return GetConceptResult(existing.Id);}var id=LogicalIds.New(u.UiaKind);var state=UiaState(u.Uia);var props=JsonDefaults.Element(new{uia=u.Uia,rootHwnd=u.RootHwnd,affinity=u.Affinity,documentedKeyProperty=kp,commands=GetOpt<JsonElement?>(p,"commands")});var c=new LogicalConcept(id,u.UiaKind,IdentityStatus.exact,state,u.ParentId,u.AppInstanceId,stable,0,0,null,props,u.Evidence);c=_store.UpsertConcept(c,DeltaKind.ConceptCreated,id);_store.UpsertBinding(id,"uia",u.Uia.ProviderEpoch,u.ParentId!,new{rootHwnd=u.RootHwnd,affinity=u.Affinity,observation=u.Uia});_store.UpsertRelation(u.ParentId!,"children",id);_store.UpsertInterest(id,"retained",new{uia=true});return GetConceptResult(c.Id);
    }
    Task<object> RetainItem(ItemCandidate i,CancellationToken ct)
    {
        var existing=_store.ListConcepts(ConceptKind.Item,includeRetired:false,stableKey:i.StableItemKey).FirstOrDefault(x=>x.ParentId==i.CollectionId);if(existing!=null)return Task.FromResult(GetConceptResult(existing.Id));var id=LogicalIds.New(ConceptKind.Item);var props=JsonDefaults.Element(new{uia=i.Uia,rootHwnd=i.RootHwnd,affinity=i.Affinity,stableItemKey=i.StableItemKey});var itemIdentity=IsVirtualizedObservation(i.Uia)?IdentityStatus.virtualized:IdentityStatus.exact;var c=new LogicalConcept(id,ConceptKind.Item,itemIdentity,UiaState(i.Uia),i.CollectionId,i.AppInstanceId,i.StableItemKey,0,0,null,props,i.Evidence);c=_store.UpsertConcept(c,DeltaKind.ConceptCreated,id);_store.UpsertBinding(id,"uia",i.Uia.ProviderEpoch,i.CollectionId,new{rootHwnd=i.RootHwnd,affinity=i.Affinity,observation=i.Uia});_store.UpsertRelation(i.CollectionId,"items",id);_store.UpsertInterest(id,"retained",new{stableItemKey=i.StableItemKey});return Task.FromResult(GetConceptResult(c.Id));
    }

    object QueryResult(List<object> list)=>new{count=list.Count,ambiguous=list.Count>1,candidates=list};
    object CandidateView(Candidate c)=>c switch
    {
        AppCandidate a=>new{id=a.Id,kind="app",identity=a.Identity.ToString(),a.Name,a.ImagePath,a.ProcessId,a.ProcessStartFileTime,a.WindowTitle,evidence=a.Evidence},
        AppInstanceCandidate a=>new{id=a.Id,kind="app_instance",identity=a.Identity.ToString(),app=a.AppId,a.ProcessId,a.ProcessStartFileTime,a.ImagePath,evidence=a.Evidence},
        WindowCandidate w=>new{id=w.Id,kind=w.WindowKind.ToString(),identity=w.Unique?"exact":"ambiguous",appInstance=w.AppInstance,parent=w.Parent,native=w.Native,uia=w.Uia,evidence=w.Evidence},
        UiaCandidate u=>new{id=u.Id,kind=u.UiaKind.ToString(),identity=u.Unique?"exact":"ambiguous",parent=u.Parent,appInstance=u.AppInstanceId,uia=u.Uia,evidence=u.Evidence},
        ItemCandidate i=>new{id=i.Id,kind="item",identity="exact",parent=i.CollectionId,key=i.StableItemKey,uia=i.Uia,evidence=i.Evidence},
        _=>new{id=c.Id,kind=c.Kind.ToString(),identity=c.Identity.ToString()}
    };

    object GetConceptResult(string id)
    {
        var c=RequireConcept(id);return new{concept=c,bindings=_store.GetBindings(id),relations=_store.GetRelations(id)};
    }
    LogicalConcept RequireConcept(string id,ConceptKind? kind=null){var c=_store.GetConcept(id)??throw new KernelException(ErrorCode.not_found,$"logical object {id} not found");if(kind!=null&&c.Kind!=kind)throw new KernelException(ErrorCode.unsupported,$"{id} is {c.Kind}, expected {kind}");return c;}
    void EnsureMutableIdentity(LogicalConcept c,bool allowVirtualized){if(c.RetiredSequence!=null||c.Identity==IdentityStatus.destroyed)throw new KernelException(ErrorCode.destroyed,$"{c.Id} destroyed");if(c.Identity==IdentityStatus.ambiguous)throw new KernelException(ErrorCode.ambiguous,$"{c.Id} ambiguous");if(c.Identity==IdentityStatus.stale)throw new KernelException(ErrorCode.stale,$"{c.Id} stale");if(c.Identity==IdentityStatus.virtualized&&!allowVirtualized)throw new KernelException(ErrorCode.virtualized,$"{c.Id} virtualized");if(c.Identity==IdentityStatus.unavailable)throw new KernelException(ErrorCode.unavailable,$"{c.Id} unavailable");}

    void RememberCandidate(Candidate c){_candidates[c.Id]=c;_candidateOrder.Enqueue(c.Id);while(_candidates.Count>4096&&_candidateOrder.TryDequeue(out var old))_candidates.TryRemove(old,out _);}
    void RememberVisual(VisualDescriptor d){_visual[d.Id]=d;_visualOrder.Enqueue(d.Id);while(_visual.Count>256&&_visualOrder.TryDequeue(out var old))_visual.TryRemove(old,out _);}
    void RememberFrame(VisualFrameRef frame,string windowId,long nativeGeneration,long desktopEpoch){_frames[frame.Id]=(frame,windowId,nativeGeneration,desktopEpoch);_frameOrder.Enqueue(frame.Id);while(_frames.Count>128&&_frameOrder.TryDequeue(out var old))_frames.TryRemove(old,out _);}    string CandidateId()=>"q_"+Guid.NewGuid().ToString("N")[..14];
    IdentityEvidence Evidence(string cls,Dictionary<string,string> w,string note)=>new(cls,w,Array.Empty<string>(),0,_desktopEpoch,note);
    static UiState UiaState(UiaElementObservation u){var s=UiState.None;if(u.Enabled)s|=UiState.Enabled;if(!u.Offscreen)s|=UiState.Visible;else s|=UiState.Offscreen;if(u.HasKeyboardFocus)s|=UiState.Focused;if(u.Selected==true)s|=UiState.Selected;return s;}
    static UiState WindowUiState(NativeWindowObservation n,UiaElementObservation? u){var s=UiState.None;if(n.Visible)s|=UiState.Visible;if(n.Minimized)s|=UiState.Minimized;if(n.Cloaked)s|=UiState.Cloaked;if(n.BlockingPopupHwnd!=0)s|=UiState.ModalBlocked;if(n.Enabled&&u?.Enabled!=false)s|=UiState.Enabled;if(u?.HasKeyboardFocus==true)s|=UiState.Focused;return s;}
    static string ProcessImagePath(uint pid){try{using var p=Process.GetProcessById((int)pid);return p.MainModule?.FileName??"";}catch{return "";}}
    static T? GetProp<T>(LogicalConcept c,string name){if(!c.Properties.TryGetProperty(name,out var e)||e.ValueKind==JsonValueKind.Null)return default;return e.Deserialize<T>(JsonDefaults.Options);}
    static T Get<T>(JsonElement e,string n)=>e.GetProperty(n).Deserialize<T>(JsonDefaults.Options)!;
    static T? GetOpt<T>(JsonElement e,string n){if(!e.TryGetProperty(n,out var v)||v.ValueKind==JsonValueKind.Null)return default;return v.Deserialize<T>(JsonDefaults.Options);}
    static long GetLong(JsonElement e,string n,long fallback=0)=>e.TryGetProperty(n,out var v)&&v.TryGetInt64(out var x)?x:fallback;
    async Task<JsonElement> SessionCall(string method,object p,int timeout,CancellationToken ct=default)=>await _session.CallAsync(method,p,timeout,ct);
    async Task<T?> SessionCall<T>(string method,object p,int timeout,CancellationToken ct=default)=>(await SessionCall(method,p,timeout,ct)).Deserialize<T>(JsonDefaults.Options);
}

internal sealed class KernelException:Exception
{
    public ErrorCode Code{get;} public int? NativeCode{get;} public string? Detail{get;}
    public KernelException(ErrorCode code,string message,int? nativeCode=null,string? detail=null):base(message){Code=code;NativeCode=nativeCode;Detail=detail;}
}