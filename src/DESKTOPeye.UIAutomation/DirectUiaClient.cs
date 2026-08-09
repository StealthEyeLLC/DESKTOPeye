using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using DESKTOPeye.Protocol;
using Interop.UIAutomationClient;
using static Interop.UIAutomationClient.UIA_EventIds;
using static Interop.UIAutomationClient.UIA_PatternIds;
using static Interop.UIAutomationClient.UIA_PropertyIds;

namespace DESKTOPeye.UIAutomation;

public sealed record UiaDirtySignal(long ProviderEpoch,string Kind,string RuntimeId,int EventId,int PropertyId,DateTimeOffset At);

public sealed class DirectUiaClient : IDisposable
{
    readonly IUIAutomation6 _uia;
    readonly long _epoch;
    readonly ConcurrentQueue<UiaDirtySignal> _dirty = new();
    readonly List<GroupSubscription> _groups = new();
    public long ProviderEpoch => _epoch;
    public DirectUiaClient(long epoch,uint connectionTimeoutMs=1500,uint transactionTimeoutMs=3000)
    {
        _epoch=epoch;
        _uia=(IUIAutomation6)new CUIAutomation8Class();
        _uia.ConnectionTimeout=connectionTimeoutMs;
        _uia.TransactionTimeout=transactionTimeoutMs;
    }
    public string[] Capabilities()=>new[]{"direct_com","uia6","grouped_events","cache_request","patterns","runtime_id_epoch_scoped"};

    IUIAutomationCacheRequest CreateProjectionCache(TreeScope scope=TreeScope.TreeScope_Subtree)
    {
        var c=_uia.CreateCacheRequest(); c.TreeScope=scope; c.TreeFilter=_uia.ControlViewCondition;
        foreach(var p in new[]{UIA_RuntimeIdPropertyId,UIA_ProcessIdPropertyId,UIA_ControlTypePropertyId,UIA_NamePropertyId,UIA_AutomationIdPropertyId,UIA_ClassNamePropertyId,UIA_ItemStatusPropertyId,UIA_FrameworkIdPropertyId,UIA_NativeWindowHandlePropertyId,UIA_IsEnabledPropertyId,UIA_IsOffscreenPropertyId,UIA_HasKeyboardFocusPropertyId,UIA_BoundingRectanglePropertyId,
            UIA_IsInvokePatternAvailablePropertyId,UIA_IsValuePatternAvailablePropertyId,UIA_IsSelectionItemPatternAvailablePropertyId,UIA_IsTogglePatternAvailablePropertyId,UIA_IsExpandCollapsePatternAvailablePropertyId,UIA_IsScrollItemPatternAvailablePropertyId,UIA_IsItemContainerPatternAvailablePropertyId,UIA_IsVirtualizedItemPatternAvailablePropertyId}) c.AddProperty(p);
        return c;
    }
    public UiaElementObservation ObserveHandle(long hwnd)
    {
        var cache=CreateProjectionCache(TreeScope.TreeScope_Element);
        var el=_uia.ElementFromHandleBuildCache(new IntPtr(hwnd),cache); return SerializeCached(el);
    }
    public UiaElementObservation Focused()
    {
        var cache=CreateProjectionCache(TreeScope.TreeScope_Element); return SerializeCached(_uia.GetFocusedElementBuildCache(cache));
    }
    public UiaElementObservation ElementFromPoint(double x,double y)
    {
        var pt=new tagPOINT{ x=(int)Math.Round(x),y=(int)Math.Round(y)}; var cache=CreateProjectionCache(TreeScope.TreeScope_Element); return SerializeCached(_uia.ElementFromPointBuildCache(pt,cache));
    }
    public IReadOnlyList<UiaElementObservation> Query(long rootHwnd,string? name=null,string? automationId=null,string? itemStatus=null,int? controlType=null,int limit=256)
    {
        var root=_uia.ElementFromHandle(new IntPtr(rootHwnd));
        var cache=CreateProjectionCache(TreeScope.TreeScope_Element);
        var arr=root.FindAllBuildCache(TreeScope.TreeScope_Descendants,_uia.ControlViewCondition,cache);
        var count=Math.Min(arr.Length,Math.Max(1,limit)); var list=new List<UiaElementObservation>();
        for(var i=0;i<count;i++)
        {
            var o=SerializeCached(arr.GetElement(i));
            if(name is not null&&!string.Equals(o.Name,name,StringComparison.Ordinal))continue;
            if(automationId is not null&&!string.Equals(o.AutomationId,automationId,StringComparison.Ordinal))continue;
            if(itemStatus is not null&&!string.Equals(o.ItemStatus,itemStatus,StringComparison.Ordinal))continue;
            if(controlType is not null&&o.ControlType!=controlType.Value)continue;
            list.Add(o);
        }
        return list;
    }
    public UiaElementObservation ResolveByRuntimeId(long rootHwnd,string runtimeId)
    {
        var matches=Query(rootHwnd,limit:1024).Where(x=>x.RuntimeId==runtimeId).ToArray();
        if(matches.Length!=1)throw new UiaResolveException(matches.Length==0?"not_found":"ambiguous"); return matches[0];
    }
    IUIAutomationElement ResolveElement(long rootHwnd,UiaLocator loc)
    {
        var root=_uia.ElementFromHandle(new IntPtr(rootHwnd)); var all=root.FindAll(TreeScope.TreeScope_Descendants,_uia.ControlViewCondition); IUIAutomationElement? found=null;
        for(var i=0;i<all.Length;i++){var e=all.GetElement(i); if(!Matches(e,loc))continue; if(found is not null)throw new UiaResolveException("ambiguous"); found=e;}
        return found??throw new UiaResolveException("not_found");
    }
    static bool Matches(IUIAutomationElement e,UiaLocator l)
    {
        if(l.RuntimeId is not null&&RuntimeId(e)!=l.RuntimeId)return false;
        if(l.AutomationId is not null&&e.CurrentAutomationId!=l.AutomationId)return false;
        if(l.Name is not null&&e.CurrentName!=l.Name)return false;
        if(l.ItemStatus is not null&&e.CurrentItemStatus!=l.ItemStatus)return false;
        if(l.ControlType is not null&&e.CurrentControlType!=l.ControlType.Value)return false;
        return true;
    }
    public UiaActionReply Action(long rootHwnd,UiaLocator locator,string operation,string? value=null)
    {
        var e=ResolveElement(rootHwnd,locator); if(e.CurrentIsEnabled==0)return new(false,"disabled",ObserveCurrent(e),null);
        object? extra=null;
        switch(operation)
        {
            case "invoke": ((IUIAutomationInvokePattern)e.GetCurrentPattern(UIA_InvokePatternId)).Invoke(); break;
            case "set_value": ((IUIAutomationValuePattern)e.GetCurrentPattern(UIA_ValuePatternId)).SetValue(value??""); break;
            case "select": ((IUIAutomationSelectionItemPattern)e.GetCurrentPattern(UIA_SelectionItemPatternId)).Select(); break;
            case "toggle": var t=(IUIAutomationTogglePattern)e.GetCurrentPattern(UIA_TogglePatternId); t.Toggle(); extra=t.CurrentToggleState.ToString(); break;
            case "expand": ((IUIAutomationExpandCollapsePattern)e.GetCurrentPattern(UIA_ExpandCollapsePatternId)).Expand(); break;
            case "collapse": ((IUIAutomationExpandCollapsePattern)e.GetCurrentPattern(UIA_ExpandCollapsePatternId)).Collapse(); break;
            case "scroll_into_view": ((IUIAutomationScrollItemPattern)e.GetCurrentPattern(UIA_ScrollItemPatternId)).ScrollIntoView(); break;
            case "realize": ((IUIAutomationVirtualizedItemPattern)e.GetCurrentPattern(UIA_VirtualizedItemPatternId)).Realize(); break;
            case "focus": e.SetFocus(); break;
            default: return new(false,"unsupported",ObserveCurrent(e),null);
        }
        return new(true,"ok",ObserveCurrent(e),extra);
    }
    public UiaElementObservation FindItemByProperty(long collectionHwnd,UiaLocator collectionLocator,int propertyId,object value)
    {
        var c=ResolveElement(collectionHwnd,collectionLocator); var p=(IUIAutomationItemContainerPattern)c.GetCurrentPattern(UIA_ItemContainerPatternId); var item=p.FindItemByProperty(null!,propertyId,value); if(item is null)throw new UiaResolveException("not_found"); return ObserveCurrent(item);
    }
    public UiaPointResult ClickablePoint(long rootHwnd,UiaLocator locator)
    {
        var e=ResolveElement(rootHwnd,locator); var pt=new tagPOINT(); var ok=e.GetClickablePoint(out pt)!=0; return new(ok,pt.x,pt.y,ObserveCurrent(e));
    }
    public GroupSubscription Subscribe(long rootHwnd,string scopeId)
    {
        var root=_uia.ElementFromHandle(new IntPtr(rootHwnd)); _uia.CreateEventHandlerGroup(out var group); var sink=new EventSink(_epoch,_dirty);
        group.AddAutomationEventHandler(UIA_StructureChangedEventId,TreeScope.TreeScope_Subtree,null!,sink);
        group.AddAutomationEventHandler(UIA_Window_WindowOpenedEventId,TreeScope.TreeScope_Subtree,null!,sink);
        group.AddAutomationEventHandler(UIA_Window_WindowClosedEventId,TreeScope.TreeScope_Subtree,null!,sink);
        group.AddAutomationEventHandler(UIA_MenuOpenedEventId,TreeScope.TreeScope_Subtree,null!,sink);
        var props=new[]{UIA_NamePropertyId,UIA_IsEnabledPropertyId,UIA_HasKeyboardFocusPropertyId,UIA_IsOffscreenPropertyId,UIA_ValueValuePropertyId,UIA_SelectionItemIsSelectedPropertyId,UIA_BoundingRectanglePropertyId};
        group.AddPropertyChangedEventHandler(TreeScope.TreeScope_Subtree,null!,sink,ref props[0],props.Length);
        _uia.AddEventHandlerGroup(root,group); var sub=new GroupSubscription(_uia,root,group,sink,scopeId,_epoch); lock(_groups)_groups.Add(sub); return sub;
    }
    public UiaDirtySignal[] DrainDirty(int max=512){var l=new List<UiaDirtySignal>();while(l.Count<max&&_dirty.TryDequeue(out var d))l.Add(d);return l.ToArray();}
    UiaElementObservation ObserveCurrent(IUIAutomationElement e)
    {
        var r=e.CurrentBoundingRectangle; return new(RuntimeId(e),e.CurrentProcessId,e.CurrentNativeWindowHandle.ToInt64(),e.CurrentName??"",e.CurrentAutomationId??"",e.CurrentItemStatus??"",e.CurrentFrameworkId??"",e.CurrentClassName??"",e.CurrentControlType,e.CurrentIsEnabled!=0,e.CurrentIsOffscreen!=0,e.CurrentHasKeyboardFocus!=0,new RectD(r.left,r.top,r.right-r.left,r.bottom-r.top),PatternNames(e),_epoch);
    }
    UiaElementObservation SerializeCached(IUIAutomationElement e)
    {
        var r=e.CachedBoundingRectangle; return new(RuntimeId(e),e.CachedProcessId,e.CachedNativeWindowHandle.ToInt64(),e.CachedName??"",e.CachedAutomationId??"",e.CachedItemStatus??"",e.CachedFrameworkId??"",e.CachedClassName??"",e.CachedControlType,e.CachedIsEnabled!=0,e.CachedIsOffscreen!=0,e.CachedHasKeyboardFocus!=0,new RectD(r.left,r.top,r.right-r.left,r.bottom-r.top),PatternNamesCached(e),_epoch);
    }
    static string RuntimeId(IUIAutomationElement e)=>string.Join(".",e.GetRuntimeId()??Array.Empty<int>());
    static string[] PatternNames(IUIAutomationElement e){var l=new List<string>(); Add(UIA_IsInvokePatternAvailablePropertyId,"invoke");Add(UIA_IsValuePatternAvailablePropertyId,"value");Add(UIA_IsSelectionItemPatternAvailablePropertyId,"selection_item");Add(UIA_IsTogglePatternAvailablePropertyId,"toggle");Add(UIA_IsExpandCollapsePatternAvailablePropertyId,"expand_collapse");Add(UIA_IsScrollItemPatternAvailablePropertyId,"scroll_item");Add(UIA_IsItemContainerPatternAvailablePropertyId,"item_container");Add(UIA_IsVirtualizedItemPatternAvailablePropertyId,"virtualized_item");return l.ToArray(); void Add(int p,string n){try{if(Convert.ToBoolean(e.GetCurrentPropertyValue(p)))l.Add(n);}catch{}}}
    static string[] PatternNamesCached(IUIAutomationElement e){var l=new List<string>(); Add(UIA_IsInvokePatternAvailablePropertyId,"invoke");Add(UIA_IsValuePatternAvailablePropertyId,"value");Add(UIA_IsSelectionItemPatternAvailablePropertyId,"selection_item");Add(UIA_IsTogglePatternAvailablePropertyId,"toggle");Add(UIA_IsExpandCollapsePatternAvailablePropertyId,"expand_collapse");Add(UIA_IsScrollItemPatternAvailablePropertyId,"scroll_item");Add(UIA_IsItemContainerPatternAvailablePropertyId,"item_container");Add(UIA_IsVirtualizedItemPatternAvailablePropertyId,"virtualized_item");return l.ToArray(); void Add(int p,string n){try{if(Convert.ToBoolean(e.GetCachedPropertyValue(p)))l.Add(n);}catch{}}}
    public void Dispose(){lock(_groups){foreach(var g in _groups.ToArray())g.Dispose();_groups.Clear();} try{_uia.RemoveAllEventHandlers();}catch{} if(Marshal.IsComObject(_uia))Marshal.FinalReleaseComObject(_uia);}

    sealed class EventSink : IUIAutomationEventHandler,IUIAutomationPropertyChangedEventHandler,IUIAutomationStructureChangedEventHandler,IUIAutomationFocusChangedEventHandler
    {
        readonly long _epoch; readonly ConcurrentQueue<UiaDirtySignal> _q; public EventSink(long e,ConcurrentQueue<UiaDirtySignal> q){_epoch=e;_q=q;}
        static string Rid(IUIAutomationElement? e){try{return e is null?"":string.Join(".",e.GetRuntimeId()??Array.Empty<int>());}catch{return "unavailable";}}
        public void HandleAutomationEvent(IUIAutomationElement sender,int eventId)=>_q.Enqueue(new(_epoch,"automation",Rid(sender),eventId,0,DateTimeOffset.UtcNow));
        public void HandlePropertyChangedEvent(IUIAutomationElement sender,int propertyId,object newValue)=>_q.Enqueue(new(_epoch,"property",Rid(sender),0,propertyId,DateTimeOffset.UtcNow));
        public void HandleStructureChangedEvent(IUIAutomationElement sender,StructureChangeType changeType,int[] runtimeId)=>_q.Enqueue(new(_epoch,"structure",Rid(sender),UIA_StructureChangedEventId,0,DateTimeOffset.UtcNow));
        public void HandleFocusChangedEvent(IUIAutomationElement sender)=>_q.Enqueue(new(_epoch,"focus",Rid(sender),UIA_AutomationFocusChangedEventId,UIA_HasKeyboardFocusPropertyId,DateTimeOffset.UtcNow));
    }
}

public sealed record UiaLocator(string? RuntimeId=null,string? AutomationId=null,string? Name=null,string? ItemStatus=null,int? ControlType=null);
public sealed record UiaActionReply(bool Success,string Status,UiaElementObservation Current,object? Extra);
public sealed record UiaPointResult(bool Available,int X,int Y,UiaElementObservation Current);
public sealed class UiaResolveException : Exception { public string Code{get;} public UiaResolveException(string code):base(code){Code=code;} }

public sealed class GroupSubscription : IDisposable
{
    readonly IUIAutomation6 _uia; readonly IUIAutomationElement _root; readonly IUIAutomationEventHandlerGroup _group; readonly object _sink; int _disposed; public string ScopeId{get;} public long ProviderEpoch{get;}
    internal GroupSubscription(IUIAutomation6 uia,IUIAutomationElement root,IUIAutomationEventHandlerGroup group,object sink,string scope,long epoch){_uia=uia;_root=root;_group=group;_sink=sink;ScopeId=scope;ProviderEpoch=epoch;}
    public void Dispose(){if(Interlocked.Exchange(ref _disposed,1)!=0)return;try{_uia.RemoveEventHandlerGroup(_root,_group);}catch{} GC.KeepAlive(_sink); if(Marshal.IsComObject(_group))Marshal.FinalReleaseComObject(_group); if(Marshal.IsComObject(_root))Marshal.FinalReleaseComObject(_root);}
}