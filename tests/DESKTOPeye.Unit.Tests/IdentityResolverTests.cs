using DESKTOPeye.Protocol;
using DESKTOPeye.World;

namespace DESKTOPeye.Unit.Tests;

public class IdentityResolverTests
{
    static WindowWitness W(long hwnd=100,long start=10,uint pid=1,uint tid=2,long gen=1,int session=1,string desktop="Default",string title="Same",long owner=0)=>new(session,desktop,hwnd,start,pid,tid,gen,"Fixture",title,owner);
    static ControlWitness C(string? key=null,string runtime="1.2",string aid="dup",long epoch=1,string parent="window_1",bool current=true,int type=50000)=>new(key,runtime,aid,"OK",type,parent,epoch,1,current);

    [Fact] public void StableNativeIncumbentIsExactWithoutGap(){var r=IdentityResolver.ResolveWindow(W(),new[]{W()},false);Assert.Equal(IdentityStatus.exact,r.Status);}
    [Fact] public void HwndCannotRebindAcrossGap(){var r=IdentityResolver.ResolveWindow(W(),new[]{W()},true);Assert.Equal(IdentityStatus.stale,r.Status);}
    [Fact] public void SameHandleDifferentGenerationIsNotSameWindow(){var r=IdentityResolver.ResolveWindow(W(),new[]{W(gen:2)},false);Assert.Equal(IdentityStatus.destroyed,r.Status);}
    [Fact] public void SameHandleSameTitleDifferentProcessIncarnationIsNotSameWindow(){var r=IdentityResolver.ResolveWindow(W(),new[]{W(start:11)},false);Assert.Equal(IdentityStatus.destroyed,r.Status);}
    [Fact] public void TitleAndOwnerSimilarityCannotRepairObservationGap(){var r=IdentityResolver.ResolveWindow(W(title:"Twin",owner:44),new[]{W(hwnd:900,gen:7,title:"Twin",owner:44)},true);Assert.Equal(IdentityStatus.stale,r.Status);}
    [Fact] public void DesktopChangeBreaksNativeIncumbent(){var r=IdentityResolver.ResolveWindow(W(),new[]{W(desktop:"Secure")},false);Assert.Equal(IdentityStatus.destroyed,r.Status);}
    [Fact] public void OperationLineageMustBeUnique(){var r=IdentityResolver.ResolveWindow(W(),new[]{W(hwnd:101),W(hwnd:102)},false,true);Assert.Equal(IdentityStatus.ambiguous,r.Status);}
    [Fact] public void RuntimeIdIsExactOnlyInsideProviderEpoch(){var r=IdentityResolver.ResolveControl(C(),new[]{C()},false,true);Assert.Equal(IdentityStatus.exact,r.Status);}
    [Fact] public void RuntimeIdWithWrongParentIsNotExact(){var r=IdentityResolver.ResolveControl(C(),new[]{C(parent:"window_2")},false,true);Assert.Equal(IdentityStatus.stale,r.Status);}
    [Fact] public void ParentReincarnationForcesChildStale(){var r=IdentityResolver.ResolveControl(C("row-42"),new[]{C("row-42",runtime:"9",epoch:2)},true,false);Assert.Equal(IdentityStatus.stale,r.Status);}
    [Fact] public void AutomationIdAloneAfterProviderRestartIsOnlyCandidate(){var r=IdentityResolver.ResolveControl(C(),new[]{C(runtime:"9.9",epoch:2)},true,true);Assert.Equal(IdentityStatus.candidate,r.Status);}
    [Fact] public void DuplicateAutomationIdAfterProviderRestartDoesNotBecomeExact(){var r=IdentityResolver.ResolveControl(C(),new[]{C(runtime:"8",epoch:2),C(runtime:"9",epoch:2)},true,true);Assert.Equal(IdentityStatus.candidate,r.Status);}
    [Fact] public void StableKeyCanReboundUnderExactParent(){var r=IdentityResolver.ResolveControl(C("row-42"),new[]{C("row-42",runtime:"8.8",epoch:2)},true,true);Assert.Equal(IdentityStatus.rebound_exact,r.Status);}
    [Fact] public void DuplicateStableKeyIsAmbiguous(){var r=IdentityResolver.ResolveControl(C("row-42"),new[]{C("row-42",runtime:"8",epoch:2),C("row-42",runtime:"9",epoch:2)},true,true);Assert.Equal(IdentityStatus.ambiguous,r.Status);}
    [Fact] public void StableKeyCollisionBeatsVisualOrTextSimilarity(){var r=IdentityResolver.ResolveControl(C("row-42",aid:"same"),new[]{C("row-42",runtime:"8",aid:"same",epoch:2),C("row-42",runtime:"9",aid:"same",epoch:2)},true,true);Assert.Equal(IdentityStatus.ambiguous,r.Status);}
    [Fact] public void MissingCurrentProviderReferenceWithoutKeyIsStale(){var r=IdentityResolver.ResolveControl(C(current:false),Array.Empty<ControlWitness>(),false,true);Assert.Equal(IdentityStatus.stale,r.Status);}
    [Fact] public void ItemRequiresDocumentedUniqueStableKey(){Assert.True(IdentityResolver.CanPromoteItem("K",true,true));Assert.False(IdentityResolver.CanPromoteItem(null,true,true));Assert.False(IdentityResolver.CanPromoteItem("K",false,true));Assert.False(IdentityResolver.CanPromoteItem("K",true,false));}
    [Fact] public void MutationEligibilityRequiresExactIdentityAncestorsEpochAndRoute(){Assert.True(IdentityResolver.MutationEligible(IdentityStatus.exact,true,true,true,true));Assert.False(IdentityResolver.MutationEligible(IdentityStatus.candidate,true,true,true,true));Assert.False(IdentityResolver.MutationEligible(IdentityStatus.exact,false,true,true,true));Assert.False(IdentityResolver.MutationEligible(IdentityStatus.exact,true,false,true,true));}
    [Fact] public void PhysicalPreflightNeverCreatesTargetBound(){Assert.Equal(AssuranceClass.race_bounded_physical,IdentityResolver.AssuranceFor(RouteKind.Pointer));Assert.Equal(AssuranceClass.race_bounded_physical,IdentityResolver.AssuranceFor(RouteKind.Keyboard));Assert.Equal(AssuranceClass.provider_semantic,IdentityResolver.AssuranceFor(RouteKind.UIA));Assert.Equal(AssuranceClass.target_bound,IdentityResolver.AssuranceFor(RouteKind.Domain,true));}
}