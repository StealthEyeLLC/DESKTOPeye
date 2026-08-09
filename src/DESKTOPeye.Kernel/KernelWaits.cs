using System.Diagnostics;
using System.Text.Json;
using DESKTOPeye.Protocol;

namespace DESKTOPeye.Kernel;

internal sealed partial class KernelService
{
    async Task<object> WaitSelection(JsonElement p,CancellationToken ct)
    {
        var target=Get<string>(p,"target");return await WaitLoop(p,async token=>{var item=RequireConcept(target,ConceptKind.Item);await ReconcileItem(item,token);item=RequireConcept(target);if(item.Identity==IdentityStatus.virtualized)return (false,(object)new{id=target,identity="virtualized",selected=false});var u=UiaObservation(UiaBinding(item));return (u.Selected==true,(object)new{id=target,identity=item.Identity.ToString(),selected=u.Selected,key=item.StableKey,providerEpoch=u.ProviderEpoch});},ct);
    }
    async Task<object> WaitCollectionEpoch(JsonElement p,CancellationToken ct)
    {
        var target=Get<string>(p,"target");var expected=GetOpt<long?>(p,"greaterThan")??GetOpt<long?>(p,"atLeast")??0;var atLeast=p.TryGetProperty("atLeast",out _);return await WaitLoop(p,async token=>{var view=await CollectionView(target,token);var e=JsonDefaults.Element(view);var epoch=GetLong(e,"viewEpoch");return (atLeast?epoch>=expected:epoch>expected,(object)view);},ct);
    }
    async Task<object> WaitControlState(JsonElement p,CancellationToken ct)
    {
        var target=Get<string>(p,"target");var expected=Get<string>(p,"identity");return await WaitLoop(p,async token=>{var c=RequireConcept(target);if(c.Kind==ConceptKind.Item)await ReconcileItem(c,token);else if(c.Kind is ConceptKind.Control or ConceptKind.Collection)await ReconcileUia(c,token);c=RequireConcept(target);return (string.Equals(c.Identity.ToString(),expected,StringComparison.OrdinalIgnoreCase),(object)new{id=c.Id,identity=c.Identity.ToString(),state=c.State.ToString()});},ct);
    }
    async Task<object> WaitEnabled(JsonElement p,CancellationToken ct)
    {
        var target=Get<string>(p,"target");var expected=GetOpt<bool?>(p,"enabled")??true;return await WaitLoop(p,async token=>{var c=RequireConcept(target);await ReconcileUia(c,token);c=RequireConcept(target);var enabled=(c.State&UiState.Enabled)!=0;return (enabled==expected,(object)new{id=c.Id,enabled,identity=c.Identity.ToString()});},ct);
    }
    async Task<object> WaitFocus(JsonElement p,CancellationToken ct)
    {
        var target=Get<string>(p,"target");return await WaitLoop(p,async token=>{await ReconstructFocusAsync(token);var c=RequireConcept(target);var focused=(c.State&UiState.Focused)!=0;return (focused,(object)new{id=c.Id,focused,identity=c.Identity.ToString()});},ct);
    }
    async Task<object> WaitValue(JsonElement p,CancellationToken ct)
    {
        return await WaitLoop(p,async token=>
        {
            var value=await ReadValueForWait(p,token);var expected=GetOpt<string>(p,"equals");var contains=GetOpt<string>(p,"contains");var starts=GetOpt<string>(p,"startsWith");bool ok=expected!=null?value==expected:contains!=null?value.Contains(contains,StringComparison.Ordinal):starts!=null?value.StartsWith(starts,StringComparison.Ordinal):!string.IsNullOrEmpty(value);return(ok,(object)new{value,expected,contains,starts});
        },ct);
    }
    async Task<string> ReadValueForWait(JsonElement p,CancellationToken ct)
    {
        if(p.TryGetProperty("target",out var te)&&te.ValueKind==JsonValueKind.String)
        {
            var c=RequireConcept(te.GetString()!);if(c.Kind==ConceptKind.Item)await ReconcileItem(c,ct);else await ReconcileUia(c,ct);c=RequireConcept(c.Id);if(c.Identity==IdentityStatus.virtualized)return "virtualized";var u=UiaObservation(UiaBinding(c));return u.Value??u.Name??"";
        }
        var parent=RequireConcept(Get<string>(p,"parent"));var root=NativeBinding(RootWindowOf(parent));var pid=(int)(GetProp<long?>(RequireConcept(parent.AppInstanceId!),"processId")??0);var arr=await SessionCall<UiaElementObservation[]>("uia.query",new{rootHwnd=root.Hwnd,affinity=$"app-{pid}",automationId=GetOpt<string>(p,"automationId"),name=GetOpt<string>(p,"name"),limit=8},1800,ct)??Array.Empty<UiaElementObservation>();if(arr.Length==0)return "";if(arr.Length>1)throw new KernelException(ErrorCode.ambiguous,"wait value query is ambiguous");return arr[0].Value??arr[0].Name??"";
    }
    async Task<object> WaitDialogExists(JsonElement p,CancellationToken ct)
    {
        return await WaitLoop(p,async token=>{var q=await WindowQuery(p,ConceptKind.Dialog,token);var e=JsonDefaults.Element(q);var count=(int)GetLong(e,"count");return(count==1,(object)q);},ct);
    }
    async Task<object> WaitDialogCloses(JsonElement p,CancellationToken ct)
    {
        var target=Get<string>(p,"target");return await WaitLoop(p,async token=>{var c=_store.GetConcept(target);if(c==null)return(true,(object)new{id=target,closed=true,identity="destroyed"});if(c.RetiredSequence==null&&c.Kind is ConceptKind.Window or ConceptKind.Dialog)await ReconcileWindow(c,null,token);c=_store.GetConcept(target);var closed=c==null||c.RetiredSequence!=null||c.Identity==IdentityStatus.destroyed;return(closed,(object)new{id=target,closed,identity=c?.Identity.ToString()??"destroyed"});},ct);
    }
    async Task<object> WaitPopup(JsonElement p,CancellationToken ct)
    {
        if(p.TryGetProperty("target",out var t)&&t.ValueKind==JsonValueKind.String)
        {
            var target=t.GetString()!;
            return await WaitLoop(p,async token=>{var c=RequireConcept(target);await ReconcileUia(c,token);c=RequireConcept(target);EnsureMutableIdentity(c,false);var u=UiaObservation(UiaBinding(c));var expanded=u.ExpandCollapseState==1;return(expanded,(object)new{id=c.Id,expanded,expandCollapseState=u.ExpandCollapseState,providerEpoch=u.ProviderEpoch});},ct);
        }
        return await WaitLoop(p,async token=>{var q=await ControlQuery(p,ConceptKind.Control,token);var e=JsonDefaults.Element(q);var count=(int)GetLong(e,"count");return(count==1,(object)q);},ct);
    }    async Task<object> WaitProviderHealth(JsonElement p,CancellationToken ct)
    {
        var affinity=GetOpt<string>(p,"affinity");var expected=GetOpt<string>(p,"health")??"healthy";return await WaitLoop(p,async token=>{var r=await SessionCall("workers.status",new{},1800,token);var arr=r.ValueKind==JsonValueKind.Array?r.EnumerateArray().ToArray():Array.Empty<JsonElement>();var matches=affinity==null?arr:arr.Where(x=>GetOpt<string>(x,"affinity")==affinity).ToArray();var ok=matches.Length>0&&matches.All(x=>string.Equals(GetOpt<string>(x,"health"),expected,StringComparison.OrdinalIgnoreCase));return(ok,(object)new{affinity,expected,workers=matches});},ct);
    }

    async Task<object> WaitLoop(JsonElement p,Func<CancellationToken,Task<(bool ok,object current)>> check,CancellationToken outer)
    {
        var timeout=GetOpt<int?>(p,"timeoutMs")??5000;using var cts=CancellationTokenSource.CreateLinkedTokenSource(outer);cts.CancelAfter(timeout);var sw=Stopwatch.StartNew();object? last=null;try
        {
            while(true)
            {
                cts.Token.ThrowIfCancellationRequested();var result=await check(cts.Token);last=result.current;if(result.ok){var final=await check(cts.Token);last=final.current;if(final.ok)return new WaitResult(true,ErrorCode.none,"predicate confirmed by current query",_store.Head,JsonDefaults.Element(final.current));}
                await Task.Delay(30,cts.Token);
            }
        }
        catch(OperationCanceledException) when(!outer.IsCancellationRequested){return new WaitResult(false,ErrorCode.timeout,$"predicate not satisfied within {sw.ElapsedMilliseconds} ms",_store.Head,JsonDefaults.Element(last??new{}));}
    }
}