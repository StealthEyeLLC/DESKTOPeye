using DESKTOPeye.Protocol;
namespace DESKTOPeye.World;

public sealed record WindowWitness(int SessionId,string Desktop,long Hwnd,long ProcessStartFileTime,uint ProcessId,uint ThreadId,long Generation,string ClassName,string Title,long OwnerHwnd);
public sealed record ControlWitness(string? StableKey,string RuntimeId,string AutomationId,string Name,int ControlType,string? ParentId,long ProviderEpoch,long AppInstanceEpoch,bool CurrentAvailable);
public sealed record Resolution<T>(IdentityStatus Status,T? Current,string EvidenceClass,string[] Reasons);

public static class IdentityResolver
{
    public static Resolution<WindowWitness> ResolveWindow(WindowWitness prior, IReadOnlyList<WindowWitness> candidates, bool observationGap, bool operationLineage=false)
    {
        if(operationLineage){ var exact=candidates.Where(c=>c.SessionId==prior.SessionId&&c.Desktop==prior.Desktop&&c.ProcessStartFileTime==prior.ProcessStartFileTime).ToArray(); return exact.Length==1?new(IdentityStatus.rebound_exact,exact[0],"operation_native_lineage",Array.Empty<string>()):new(IdentityStatus.ambiguous,default,"operation_native_lineage",new[]{"successor_not_unique"}); }
        var sameGeneration=candidates.Where(c=>c.SessionId==prior.SessionId&&c.Desktop==prior.Desktop&&c.Hwnd==prior.Hwnd&&c.ProcessStartFileTime==prior.ProcessStartFileTime&&c.ThreadId==prior.ThreadId&&c.Generation==prior.Generation).ToArray();
        if(!observationGap&&sameGeneration.Length==1) return new(IdentityStatus.exact,sameGeneration[0],"native_generation_incumbent",Array.Empty<string>());
        if(observationGap) return new(IdentityStatus.stale,default,"native_gap_no_generation_lease",new[]{"observation_gap","hwnd_recyclable"});
        return new(IdentityStatus.destroyed,default,"observed_generation_end",new[]{"incarnation_absent"});
    }
    public static Resolution<ControlWitness> ResolveControl(ControlWitness prior,IReadOnlyList<ControlWitness> candidates,bool providerRestart,bool parentExact)
    {
        if(!parentExact) return new(IdentityStatus.stale,default,"ancestor_not_exact",new[]{"parent_not_exact"});
        if(!providerRestart){ var same=candidates.Where(c=>c.ProviderEpoch==prior.ProviderEpoch&&c.RuntimeId==prior.RuntimeId&&c.ParentId==prior.ParentId&&c.CurrentAvailable).ToArray(); if(same.Length==1)return new(IdentityStatus.exact,same[0],"same_epoch_runtime_incumbent",Array.Empty<string>()); }
        if(!string.IsNullOrWhiteSpace(prior.StableKey)){ var key=candidates.Where(c=>c.StableKey==prior.StableKey&&c.ParentId==prior.ParentId&&c.CurrentAvailable).ToArray(); if(key.Length==1)return new(IdentityStatus.rebound_exact,key[0],"documented_scoped_stable_key",Array.Empty<string>()); if(key.Length>1)return new(IdentityStatus.ambiguous,default,"stable_key_collision",new[]{"multiple_key_candidates"}); }
        var auto=candidates.Where(c=>!string.IsNullOrEmpty(prior.AutomationId)&&c.AutomationId==prior.AutomationId&&c.ParentId==prior.ParentId&&c.ControlType==prior.ControlType&&c.CurrentAvailable).ToArray();
        if(providerRestart) return auto.Length>0?new(IdentityStatus.candidate,default,"automation_id_insufficient_after_provider_gap",new[]{"provider_epoch_changed"}):new(IdentityStatus.stale,default,"no_exact_reconstruction_evidence",new[]{"provider_epoch_changed"});
        return new(IdentityStatus.stale,default,"incumbent_unavailable",new[]{"runtime_binding_missing"});
    }
    public static bool CanPromoteItem(string? stableKey,bool uniqueUnderExactCollection,bool documentedKey)=>!string.IsNullOrWhiteSpace(stableKey)&&uniqueUnderExactCollection&&documentedKey;
    public static AssuranceClass AssuranceFor(RouteKind route,bool apiLifetimeBound=false)=>apiLifetimeBound?AssuranceClass.target_bound:route switch{RouteKind.UIA=>AssuranceClass.provider_semantic,RouteKind.Pointer or RouteKind.VisualPointer or RouteKind.Keyboard=>AssuranceClass.race_bounded_physical,_=>AssuranceClass.delivery_uncertain};
    public static bool MutationEligible(IdentityStatus identity,bool ancestorsExact,bool epochsMatch,bool enabled,bool routePreflight)=> (identity==IdentityStatus.exact||identity==IdentityStatus.rebound_exact)&&ancestorsExact&&epochsMatch&&enabled&&routePreflight;
}