using DESKTOPeye.Protocol;
using DESKTOPeye.World;

namespace DESKTOPeye.Unit.Tests;

public class IdentityResolverTests
{
    static WindowWitness W(long hwnd=100,long start=10,uint pid=1,uint tid=2,long gen=1)=>new(1,"Default",hwnd,start,pid,tid,gen,"Fixture","Same",0);
    static ControlWitness C(string? key=null,string runtime="1.2",string aid="dup",long epoch=1,string parent="window_1")=>new(key,runtime,aid,"OK",50000,parent,epoch,1,true);
    [Fact] public void HwndCannotRebindAcrossGap(){var r=IdentityResolver.ResolveWindow(W(),new[]{W()},true);Assert.Equal(IdentityStatus.stale,r.Status);}
    [Fact] public void SameHandleDifferentGenerationIsNotSameWindow(){var r=IdentityResolver.ResolveWindow(W(),new[]{W(gen:2)},false);Assert.Equal(IdentityStatus.destroyed,r.Status);}
    [Fact] public void RuntimeIdIsExactOnlyInsideProviderEpoch(){var r=IdentityResolver.ResolveControl(C(),new[]{C()},false,true);Assert.Equal(IdentityStatus.exact,r.Status);}
    [Fact] public void AutomationIdAloneAfterProviderRestartIsOnlyCandidate(){var r=IdentityResolver.ResolveControl(C(),new[]{C(runtime:"9.9",epoch:2)},true,true);Assert.Equal(IdentityStatus.candidate,r.Status);}
    [Fact] public void StableKeyCanReboundUnderExactParent(){var r=IdentityResolver.ResolveControl(C("row-42"),new[]{C("row-42",runtime:"8.8",epoch:2)},true,true);Assert.Equal(IdentityStatus.rebound_exact,r.Status);}
    [Fact] public void DuplicateStableKeyIsAmbiguous(){var r=IdentityResolver.ResolveControl(C("row-42"),new[]{C("row-42",runtime:"8",epoch:2),C("row-42",runtime:"9",epoch:2)},true,true);Assert.Equal(IdentityStatus.ambiguous,r.Status);}
    [Fact] public void ItemRequiresDocumentedUniqueStableKey(){Assert.True(IdentityResolver.CanPromoteItem("K",true,true));Assert.False(IdentityResolver.CanPromoteItem(null,true,true));Assert.False(IdentityResolver.CanPromoteItem("K",false,true));}
    [Fact] public void PhysicalPreflightNeverCreatesTargetBound(){Assert.Equal(AssuranceClass.race_bounded_physical,IdentityResolver.AssuranceFor(RouteKind.Pointer));Assert.Equal(AssuranceClass.provider_semantic,IdentityResolver.AssuranceFor(RouteKind.UIA));Assert.Equal(AssuranceClass.target_bound,IdentityResolver.AssuranceFor(RouteKind.Domain,true));}
}