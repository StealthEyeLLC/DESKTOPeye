using System.Text.Json;
using DESKTOPeye.Protocol;
using DESKTOPeye.World;

namespace DESKTOPeye.Kernel;

internal sealed partial class KernelService
{
    NativeWindowObservation NativeBinding(LogicalConcept c)
    {
        var b=_store.GetBinding(c.Id,"native")??throw new KernelException(ErrorCode.unavailable,$"{c.Id} has no native binding"); if(!b.Available)throw new KernelException(ErrorCode.unavailable,$"{c.Id} native binding unavailable"); return b.Witness.Deserialize<NativeWindowObservation>(JsonDefaults.Options)??throw new KernelException(ErrorCode.native_error,"invalid native binding");
    }
    WorldBinding UiaBinding(LogicalConcept c)
    {
        var b=_store.GetBinding(c.Id,"uia")??throw new KernelException(ErrorCode.unavailable,$"{c.Id} has no UIA binding"); if(!b.Available)throw new KernelException(ErrorCode.unavailable,$"{c.Id} UIA binding unavailable"); return b;
    }
    static UiaElementObservation UiaObservation(WorldBinding b)=>b.Witness.GetProperty("observation").Deserialize<UiaElementObservation>(JsonDefaults.Options)!;
    static long GetWitnessLong(WorldBinding b,string name)=>b.Witness.GetProperty(name).GetInt64();
    static string GetWitnessString(WorldBinding b,string name)=>b.Witness.GetProperty(name).GetString()!;

    LogicalConcept? FindExistingWindow(WindowCandidate w,string? stable)
    {
        var same=_store.ListConcepts(w.WindowKind,includeRetired:false).Where(x=>x.AppInstanceId==w.AppInstance).ToArray();
        var incumbent=same.Where(x=>{var b=_store.GetBinding(x.Id,"native");if(b is null||!b.Available)return false;var n=b.Witness.Deserialize<NativeWindowObservation>(JsonDefaults.Options);return n!=null&&n.Hwnd==w.Native.Hwnd&&n.NativeGeneration==w.Native.NativeGeneration&&n.ProcessStartFileTime==w.Native.ProcessStartFileTime;}).ToArray();
        if(incumbent.Length==1)return incumbent[0];
        if(stable!=null){var keyed=same.Where(x=>x.StableKey==stable).ToArray();if(keyed.Length==1)return keyed[0];}
        return null;
    }
    LogicalConcept? FindExistingUia(UiaCandidate u,string? stable)
    {
        var same=_store.ListConcepts(u.UiaKind,parent:u.ParentId,includeRetired:false).ToArray(); var incumbent=same.Where(x=>{var b=_store.GetBinding(x.Id,"uia");if(b is null||!b.Available)return false;var o=UiaObservation(b);return o.ProviderEpoch==u.Uia.ProviderEpoch&&o.RuntimeId==u.Uia.RuntimeId;}).ToArray(); if(incumbent.Length==1)return incumbent[0]; if(stable!=null){var keyed=same.Where(x=>x.StableKey==stable).ToArray();if(keyed.Length==1)return keyed[0];}return null;
    }

    async Task RebindWindow(LogicalConcept old,WindowCandidate w,IdentityStatus desired,CancellationToken ct)
    {
        var actual=old.Identity==IdentityStatus.stale&&old.StableKey!=null?IdentityStatus.rebound_exact:desired;var kp=GetProp<string>(old,"documentedKeyProperty");var props=JsonDefaults.Element(new{native=w.Native,uia=w.Uia,affinity=$"app-{w.Native.ProcessId}",documentedKeyProperty=kp});var state=WindowUiState(w.Native,w.Uia);bool changed=old.Identity!=actual||old.State!=state||old.Properties.GetRawText()!=props.GetRawText();var next=old with{Identity=actual,State=state,Properties=props,Evidence=w.Evidence with{Class=actual==IdentityStatus.rebound_exact?"documented_scoped_stable_key_reconstruction":w.Evidence.Class,Note=actual==IdentityStatus.rebound_exact?"exact reconstruction under exact app instance":w.Evidence.Note}};if(changed)_store.UpsertConcept(next,DeltaKind.ConceptChanged,old.Id,new{identity=actual,state});_store.UpsertBinding(old.Id,"native",_sessionHostEpoch,old.Id,w.Native,true);if(w.Uia!=null)_store.UpsertBinding(old.Id,"uia",w.Uia.ProviderEpoch,old.Id,new{rootHwnd=w.Native.Hwnd,affinity=$"app-{w.Native.ProcessId}",observation=w.Uia},true);await EnsureSubscription(next,w.Native.Hwnd,w.Native.ProcessId,ct);
    }
    async Task RebindUia(LogicalConcept old,UiaElementObservation u,long root,string affinity,IdentityStatus desired,CancellationToken ct)
    {
        var actual=old.Identity==IdentityStatus.stale&&old.StableKey!=null?IdentityStatus.rebound_exact:desired;var kp=GetProp<string>(old,"documentedKeyProperty");JsonElement? commands=null;if(old.Properties.TryGetProperty("commands",out var ce)&&ce.ValueKind!=JsonValueKind.Null)commands=ce.Clone();var props=JsonDefaults.Element(new{uia=u,rootHwnd=root,affinity,documentedKeyProperty=kp,commands});var state=UiaState(u);var parent=old.ParentId is null?null:_store.GetConcept(old.ParentId);if(parent!=null&&(parent.State&UiState.ModalBlocked)!=0)state|=UiState.ModalBlocked;bool changed=old.Identity!=actual||old.State!=state||old.Properties.GetRawText()!=props.GetRawText();var next=old with{Identity=actual,State=state,Properties=props,Evidence=new(actual==IdentityStatus.rebound_exact?"documented_scoped_stable_key_reconstruction":"same_epoch_runtime_incumbent",new(){["runtimeId"]=u.RuntimeId,["providerEpoch"]=u.ProviderEpoch.ToString(),["parent"]=old.ParentId??""},Array.Empty<string>(),u.ProviderEpoch,_desktopEpoch,actual==IdentityStatus.rebound_exact?"reconstructed by declared key under exact ancestor":"current provider incumbent")};if(changed)_store.UpsertConcept(next,DeltaKind.ConceptChanged,old.Id,new{identity=actual,state,value=u.Value,selected=u.Selected});_store.UpsertBinding(old.Id,"uia",u.ProviderEpoch,old.ParentId??old.Id,new{rootHwnd=root,affinity,observation=u},true);await Task.CompletedTask;
    }

    async Task RecoverRetainedAsync()
    {
        var native=await SessionCall<NativeWindowObservation[]>("native.snapshot",new{},10000)??Array.Empty<NativeWindowObservation>();
        foreach(var ai in _store.ListConcepts(ConceptKind.AppInstance,includeRetired:false).ToArray())
        {
            try{await ReconcileShellProcessAuthority(ai,native,CancellationToken.None);}catch(Exception ex){MarkAppInstanceUnavailable(ai,$"SHELLeye recovery check failed: {ex.Message}");}
        }
        foreach(var w in _store.ListConcepts(includeRetired:false).Where(c=>c.Kind is ConceptKind.Window or ConceptKind.Dialog).OrderBy(c=>c.CreatedSequence).ToArray())
            try{await ReconcileWindow(w,native,CancellationToken.None);}catch{}
        foreach(var u in _store.ListConcepts(includeRetired:false).Where(c=>c.Kind is ConceptKind.Control or ConceptKind.Collection).OrderBy(c=>c.CreatedSequence).ToArray())
            try{await ReconcileUia(u,CancellationToken.None);}catch{}
        foreach(var i in _store.ListConcepts(ConceptKind.Item,includeRetired:false).ToArray())try{await ReconcileItem(i,CancellationToken.None);}catch{}
    }

    void RetireTree(string id,string reason)
    {
        var c=_store.GetConcept(id);if(c==null||c.RetiredSequence!=null)return;foreach(var child in _store.ListConcepts(includeRetired:false).Where(x=>x.ParentId==id||x.AppInstanceId==id).ToArray())RetireTree(child.Id,reason);_store.Retire(id,IdentityStatus.destroyed,reason,id);
    }

    async Task<bool> RefreshModalState(LogicalConcept window,CancellationToken ct)
    {
        if(window.Kind is not (ConceptKind.Window or ConceptKind.Dialog))window=RootWindowOf(window);
        await ReconcileWindow(window,null,ct);window=RequireConcept(window.Id);if(window.RetiredSequence!=null||window.Identity is not (IdentityStatus.exact or IdentityStatus.rebound_exact))return false;
        var n=NativeBinding(window);bool enabled=true;var ub=_store.GetBinding(window.Id,"uia");if(ub is {Available:true}){try{enabled=UiaObservation(ub).Enabled;}catch{}}
        var snapshot=await SessionCall<NativeWindowObservation[]>("native.snapshot",new{},1800,ct)??Array.Empty<NativeWindowObservation>();
        var blocked=!enabled&&snapshot.Any(x=>x.Visible&&x.OwnerHwnd==n.Hwnd&&x.ProcessId==n.ProcessId&&x.ProcessStartFileTime==n.ProcessStartFileTime);
        SetModalFlag(window.Id,blocked);
        foreach(var c in _store.ListConcepts(includeRetired:false).Where(x=>x.AppInstanceId==window.AppInstanceId&&x.Id!=window.Id&&IsDescendantOf(x,window.Id)).ToArray())SetModalFlag(c.Id,blocked);
        return blocked;
    }
    bool IsDescendantOf(LogicalConcept c,string ancestor)
    {
        var seen=new HashSet<string>(StringComparer.Ordinal);var p=c.ParentId;
        while(p!=null&&seen.Add(p)){if(p==ancestor)return true;p=_store.GetConcept(p)?.ParentId;}
        return false;
    }
    void SetModalFlag(string id,bool blocked)
    {
        var c=_store.GetConcept(id);if(c==null||c.RetiredSequence!=null)return;var next=blocked?c.State|UiState.ModalBlocked:c.State&~UiState.ModalBlocked;if(next!=c.State)_store.UpsertConcept(c with{State=next},DeltaKind.ModalChanged,id,new{modalBlocked=blocked});
    }
    async Task ReconcileAppInstance(LogicalConcept ai,CancellationToken ct)
    {
        if(ai.RetiredSequence!=null)return;var native=await SessionCall<NativeWindowObservation[]>("native.snapshot",new{},1800,ct)??Array.Empty<NativeWindowObservation>();await ReconcileShellProcessAuthority(ai,native,ct);
    }
    async Task ReconcileWindow(LogicalConcept w,NativeWindowObservation[]? snapshot,CancellationToken ct)
    {
        if(w.RetiredSequence!=null)return;var ai=w.AppInstanceId is null?null:_store.GetConcept(w.AppInstanceId);if(ai==null||ai.Identity is not (IdentityStatus.exact or IdentityStatus.rebound_exact)){if(w.Identity!=IdentityStatus.stale)_store.UpsertConcept(w with{Identity=IdentityStatus.stale},DeltaKind.BindingChanged,w.Id,new{reason="ancestor_not_exact"});return;}
        snapshot??=await SessionCall<NativeWindowObservation[]>("native.snapshot",new{},2500,ct)??Array.Empty<NativeWindowObservation>();var pid=(uint)(GetProp<long?>(ai,"processId")??0);var start=GetProp<long?>(ai,"processStartFileTime")??0;
        var oldNb=_store.GetBinding(w.Id,"native"); if(!_coldGap&&oldNb is {Available:true})
        {
            var old=oldNb.Witness.Deserialize<NativeWindowObservation>(JsonDefaults.Options)!;var now=snapshot.Where(n=>n.Hwnd==old.Hwnd&&n.ProcessId==pid&&n.ProcessStartFileTime==start&&n.NativeGeneration==old.NativeGeneration).ToArray();if(now.Length==1){UiaElementObservation? u=null;try{u=await SessionCall<UiaElementObservation>("uia.observe_handle",new{hwnd=now[0].Hwnd,affinity=$"app-{pid}"},2500,ct);}catch{}var wc=new WindowCandidate("reconcile",w.Kind,ai.Id,w.ParentId,now[0],u,true,Evidence("native_generation_incumbent",new(){["hwnd"]=now[0].Hwnd.ToString(),["generation"]=now[0].NativeGeneration.ToString()},"uninterrupted Session Host generation"));await RebindWindow(w,wc,IdentityStatus.exact,ct);return;}
        }
        if(w.StableKey!=null)
        {
            var matches=new List<WindowCandidate>();foreach(var n in snapshot.Where(n=>n.ProcessId==pid&&n.ProcessStartFileTime==start))
            {
                try{var u=await SessionCall<UiaElementObservation>("uia.observe_handle",new{hwnd=n.Hwnd,affinity=$"app-{pid}"},2200,ct);if(u!=null&&KeyMatches(w.StableKey,u)){matches.Add(new("reconcile",w.Kind,ai.Id,w.ParentId,n,u,true,Evidence("documented_scoped_stable_key",new(){["stableKey"]=w.StableKey,["appinst"]=ai.Id},"unique key under exact app instance")));}}catch{}
            }
            if(matches.Count==1){await RebindWindow(w,matches[0],IdentityStatus.rebound_exact,ct);return;}if(matches.Count>1){_store.UpsertConcept(w with{Identity=IdentityStatus.ambiguous},DeltaKind.BindingChanged,w.Id,new{reason="stable_key_collision"});return;}
        }
        var previouslyAvailable=oldNb?.Available==true; if(previouslyAvailable&&!_coldGap){RetireTree(w.Id,"observed native incarnation absent");}else if(w.Identity!=IdentityStatus.stale)_store.UpsertConcept(w with{Identity=IdentityStatus.stale},DeltaKind.BindingChanged,w.Id,new{reason="no exact window reconstruction evidence"});
    }

    async Task ReconcileUia(LogicalConcept c,CancellationToken ct)
    {
        if(c.RetiredSequence!=null)return;var parent=c.ParentId is null?null:_store.GetConcept(c.ParentId);if(parent==null||parent.Identity is not (IdentityStatus.exact or IdentityStatus.rebound_exact)){if(c.Identity!=IdentityStatus.stale)_store.UpsertConcept(c with{Identity=IdentityStatus.stale},DeltaKind.BindingChanged,c.Id,new{reason="ancestor_not_exact"});return;}var nb=NativeBinding(parent);var pid=(int)(GetProp<long?>(RequireConcept(c.AppInstanceId!),"processId")??0);var affinity=$"app-{pid}";var old=_store.GetBinding(c.Id,"uia");
        if(!_coldGap&&old is {Available:true})
        {
            var oo=UiaObservation(old);try{var now=await SessionCall<UiaElementObservation>("uia.resolve_runtime",new{rootHwnd=nb.Hwnd,affinity,runtimeId=oo.RuntimeId},2200,ct);if(now!=null&&now.ProviderEpoch==oo.ProviderEpoch){await RebindUia(c,now,nb.Hwnd,affinity,IdentityStatus.exact,ct);return;}}catch{}
        }
        if(c.StableKey!=null)
        {
            var (property,value)=ParseStableKey(c.StableKey); object? q=property switch{"automationId"=>new{rootHwnd=nb.Hwnd,affinity,name=(string?)null,automationId=(string?)value,itemStatus=(string?)null,controlType=(int?)null,limit=16},"itemStatus"=>new{rootHwnd=nb.Hwnd,affinity,name=(string?)null,automationId=(string?)null,itemStatus=(string?)value,controlType=(int?)null,limit=16},_=>null};if(q!=null){try{var arr=await SessionCall<UiaElementObservation[]>("uia.query",q,2500,ct)??Array.Empty<UiaElementObservation>();if(arr.Length==1){await RebindUia(c,arr[0],nb.Hwnd,affinity,IdentityStatus.rebound_exact,ct);return;}if(arr.Length>1){_store.UpsertConcept(c with{Identity=IdentityStatus.ambiguous},DeltaKind.BindingChanged,c.Id,new{reason="stable_key_not_unique"});return;}}catch{}}
        }
        if(c.Identity!=IdentityStatus.stale)_store.UpsertConcept(c with{Identity=IdentityStatus.stale},DeltaKind.BindingChanged,c.Id,new{reason="provider binding absent and no strong reconstruction key"});
    }

    async Task ReconcileItem(LogicalConcept item,CancellationToken ct)
    {
        if(item.RetiredSequence!=null)return;var col=item.ParentId is null?null:_store.GetConcept(item.ParentId);if(col==null||col.Identity is not (IdentityStatus.exact or IdentityStatus.rebound_exact)){if(item.Identity!=IdentityStatus.stale)_store.UpsertConcept(item with{Identity=IdentityStatus.stale},DeltaKind.BindingChanged,item.Id,new{reason="collection_not_exact"});return;}if(string.IsNullOrWhiteSpace(item.StableKey)){if(item.Identity!=IdentityStatus.stale)_store.UpsertConcept(item with{Identity=IdentityStatus.stale},DeltaKind.BindingChanged,item.Id,new{reason="item_has_no_durable_key"});return;}
        var cb=UiaBinding(col);var co=UiaObservation(cb);var root=GetWitnessLong(cb,"rootHwnd");var affinity=GetWitnessString(cb,"affinity");try{var u=await SessionCall<UiaElementObservation>("uia.find_item_by_property",new{rootHwnd=root,affinity,collectionLocator=new{runtimeId=co.RuntimeId},propertyId=30005,value=item.StableKey},2600,ct);if(u!=null){if(IsVirtualizedObservation(u)){var props=JsonDefaults.Element(new{uia=u,rootHwnd=root,affinity,stableItemKey=item.StableKey});_store.UpsertBinding(item.Id,"uia",u.ProviderEpoch,item.ParentId!,new{rootHwnd=root,affinity,observation=u},true);if(item.Identity!=IdentityStatus.virtualized||item.Properties.GetRawText()!=props.GetRawText())_store.UpsertConcept(item with{Identity=IdentityStatus.virtualized,State=UiaState(u)|UiState.Offscreen,Properties=props},DeltaKind.ConceptChanged,item.Id,new{identity="virtualized",key=item.StableKey});return;}await RebindUia(item,u,root,affinity,IdentityStatus.rebound_exact,ct);return;}}catch(RpcCallException r) when(r.Error.Code==ErrorCode.not_found){}
        if(item.Identity!=IdentityStatus.virtualized)_store.UpsertConcept(item with{Identity=IdentityStatus.virtualized,State=item.State|UiState.Offscreen},DeltaKind.ConceptChanged,item.Id,new{identity="virtualized",key=item.StableKey});
    }

    async Task<object> WorldSync(JsonElement p,CancellationToken ct)
    {
        var scope=GetOpt<string>(p,"scope");var caller=GetOpt<long?>(p,"cursor")??_store.Head;var unresolved=new List<object>();if(scope!=null){try{await ReconcileScope(scope,ct);}catch(Exception ex){unresolved.Add(new{scope,error=ex.Message});}}else foreach(var i in _store.ListInterests()){try{await ReconcileScope(i.ConceptId,ct);}catch(Exception ex){unresolved.Add(new{scope=i.ConceptId,error=ex.Message});}}await ReconstructFocusAsync(ct);var read=_store.ReadDeltas(caller,GetOpt<int?>(p,"maxDeltas")??256,scope);return new{cursor=_store.Head,read,unresolvedProviderHealth=unresolved,gapConsumed=read.Gap,scope=scope??"interests"};
    }
    async Task ReconcileScope(string id,CancellationToken ct)
    {
        var c=RequireConcept(id);if(c.Kind==ConceptKind.AppInstance){await ReconcileAppInstance(c,ct);}else if(c.Kind is ConceptKind.Window or ConceptKind.Dialog){await ReconcileWindow(c,null,ct);foreach(var x in _store.ListConcepts(parent:id,includeRetired:false).ToArray()){if(x.Kind is ConceptKind.Control or ConceptKind.Collection)await ReconcileUia(x,ct);if(x.Kind==ConceptKind.Collection)foreach(var i in _store.ListConcepts(ConceptKind.Item,parent:x.Id,includeRetired:false))await ReconcileItem(i,ct);}}else if(c.Kind is ConceptKind.Control or ConceptKind.Collection)await ReconcileUia(c,ct);else if(c.Kind==ConceptKind.Item)await ReconcileItem(c,ct);
    }

    async Task EnsureSubscription(LogicalConcept window,long hwnd,uint pid,CancellationToken ct)
    {
        if(_eventSubscriptions.ContainsKey(window.Id))return;try{var r=await SessionCall("uia.subscribe",new{rootHwnd=hwnd,scopeId=window.Id,affinity=$"events-{pid}"},2500,ct);_eventSubscriptions[window.Id]=GetLong(r,"providerEpoch");}catch{}
    }
    async Task ObservationPumpAsync()
    {
        while(true)
        {
            try
            {
                await RefreshSessionState();
                var signals=await SessionCall<NativeSignalWire[]>("native.signals",new{max=256},1000)??Array.Empty<NativeSignalWire>();if(signals.Length>0)
                {
                    var retained=_store.ListConcepts(includeRetired:false).Where(c=>c.Kind is ConceptKind.Window or ConceptKind.Dialog).ToArray();foreach(var s in signals){var hit=retained.Where(c=>{var b=_store.GetBinding(c.Id,"native");if(b==null)return false;var n=b.Witness.Deserialize<NativeWindowObservation>(JsonDefaults.Options);return n?.Hwnd==s.Hwnd;}).ToArray();if(hit.Length==0){try{await DiscoverUnexpectedNativeWindow(s,CancellationToken.None);}catch{}}else foreach(var c in hit)try{await ReconcileScope(c.Id,CancellationToken.None);}catch{}}
                    _store.AppendDelta(DeltaKind.Reconciled,"native",null,new{dirtySignals=signals.Length});
                }
                foreach(var sub in _eventSubscriptions.ToArray())
                {
                    var w=_store.GetConcept(sub.Key);if(w==null||w.RetiredSequence!=null)continue;var pid=(int)(GetProp<long?>(RequireConcept(w.AppInstanceId!),"processId")??0);try{var dirty=await SessionCall<UiaDirtySignalWire[]>("uia.dirty",new{affinity=$"events-{pid}",max=128},1000)??Array.Empty<UiaDirtySignalWire>();var current=dirty.Where(d=>d.ProviderEpoch==sub.Value).ToArray();if(current.Length>0){await ReconcileScope(w.Id,CancellationToken.None);_store.AppendDelta(DeltaKind.Reconciled,w.Id,null,new{dirtySignals=current.Length,providerEpoch=sub.Value});}}catch{}
                }
            }
            catch{}
            await Task.Delay(100);
        }
    }


    async Task DiscoverUnexpectedNativeWindow(NativeSignalWire signal,CancellationToken ct)
    {
        NativeWindowObservation? native;
        try{native=await SessionCall<NativeWindowObservation>("native.window",new{hwnd=signal.Hwnd},1200,ct);}catch{return;}
        if(native==null||!native.Visible)return;
        var appinst=_store.ListConcepts(ConceptKind.AppInstance,includeRetired:false).Where(ai=>ai.Identity is IdentityStatus.exact or IdentityStatus.rebound_exact).Where(ai=>(uint)(GetProp<long?>(ai,"processId")??0)==native.ProcessId&&(GetProp<long?>(ai,"processStartFileTime")??0)==native.ProcessStartFileTime).ToArray();
        if(appinst.Length!=1)return;
        string? ownerId=null;
        if(native.OwnerHwnd!=0)
        {
            var owners=_store.ListConcepts(includeRetired:false).Where(c=>c.Kind is ConceptKind.Window or ConceptKind.Dialog).Where(c=>{var b=_store.GetBinding(c.Id,"native");if(b is not {Available:true})return false;var n=b.Witness.Deserialize<NativeWindowObservation>(JsonDefaults.Options);return n?.Hwnd==native.OwnerHwnd;}).ToArray();
            if(owners.Length==1)ownerId=owners[0].Id;
        }
        UiaElementObservation? uia=null;try{uia=await SessionCall<UiaElementObservation>("uia.observe_handle",new{hwnd=native.Hwnd,affinity=$"app-{native.ProcessId}"},1800,ct);}catch{}
        var kind=native.OwnerHwnd!=0?ConceptKind.Dialog:ConceptKind.Window;
        var evidence=Evidence("session_native_signal_discovery",new(){["appinst"]=appinst[0].Id,["hwnd"]=native.Hwnd.ToString(),["generation"]=native.NativeGeneration.ToString(),["signalSequence"]=signal.Sequence.ToString()},"unexpected current top-level native manifestation discovered by session sentinel");
        var candidate=new WindowCandidate("native-signal",kind,appinst[0].Id,ownerId,native,uia,true,evidence);
        await _mutation.WaitAsync(ct);try{await RetainWindow(candidate,JsonDefaults.Element(new{}),ct);}finally{_mutation.Release();}
    }    async Task ReconstructFocusAsync(CancellationToken ct=default)
    {
        string? focusedId=null;foreach(var w in _store.ListConcepts(includeRetired:false).Where(c=>c.Kind is ConceptKind.Window or ConceptKind.Dialog&&c.Identity is IdentityStatus.exact or IdentityStatus.rebound_exact))
        {
            if(w.AppInstanceId==null)continue;var pid=(int)(GetProp<long?>(RequireConcept(w.AppInstanceId),"processId")??0);try{var f=await SessionCall<UiaElementObservation>("uia.focused",new{affinity=$"app-{pid}"},1200,ct);if(f==null||f.ProcessId!=pid)continue;var match=_store.ListConcepts(includeRetired:false).Where(c=>c.AppInstanceId==w.AppInstanceId&&c.Kind is ConceptKind.Control or ConceptKind.Collection or ConceptKind.Item).FirstOrDefault(c=>{var b=_store.GetBinding(c.Id,"uia");return b is {Available:true}&&UiaObservation(b).ProviderEpoch==f.ProviderEpoch&&UiaObservation(b).RuntimeId==f.RuntimeId;});if(match!=null){focusedId=match.Id;break;}}catch{}
        }
        foreach(var c in _store.ListConcepts(includeRetired:false).Where(c=>c.Kind is ConceptKind.Control or ConceptKind.Collection or ConceptKind.Item))
        {
            var should=c.Id==focusedId;var has=(c.State&UiState.Focused)!=0;if(should!=has){var ns=should?c.State|UiState.Focused:c.State&~UiState.Focused;_store.UpsertConcept(c with{State=ns},DeltaKind.FocusChanged,c.Id,new{focused=should});}
        }
    }

    static bool KeyMatches(string stable,UiaElementObservation u){var (p,v)=ParseStableKey(stable);return p=="automationId"?u.AutomationId==v:p=="itemStatus"?u.ItemStatus==v:false;}
    static (string property,string value) ParseStableKey(string stable){var parts=stable.Split(':',3);return parts.Length==3&&(parts[0]=="uia")?(parts[1],parts[2]):("",stable);}
    object ExpireCursor(JsonElement p){_store.TrimDeltas(GetOpt<int?>(p,"retain")??3);return new{floor=_store.DeltaFloor,head=_store.Head};}
}
