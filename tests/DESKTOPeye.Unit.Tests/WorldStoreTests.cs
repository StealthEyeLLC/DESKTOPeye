using DESKTOPeye.Protocol;
using DESKTOPeye.World;
namespace DESKTOPeye.Unit.Tests;

public class WorldStoreTests
{
    static IdentityEvidence E=>new("test",new(),Array.Empty<string>(),1,1,"unit");
    [Fact] public void WalAndQuickCheckAreHealthy(){var d=Temp();using var s=new WorldStore(d);Assert.Equal("wal",s.JournalMode,StringComparer.OrdinalIgnoreCase);Assert.Equal("ok",s.QuickCheck);}
    [Fact] public void ConceptCommitAdvancesOneWorldSequence(){var d=Temp();using var s=new WorldStore(d);var c=new LogicalConcept("control_x",ConceptKind.Control,IdentityStatus.exact,UiState.Enabled,null,null,null,0,0,null,JsonDefaults.Element(new{name="x"}),E);var x=s.UpsertConcept(c,DeltaKind.ConceptCreated,"test");Assert.Equal(1,x.UpdatedSequence);Assert.Equal(1,s.Head);Assert.Single(s.ReadDeltas(0).Deltas);}
    [Fact] public void ExpiredCursorReturnsExplicitGap(){var d=Temp();using var s=new WorldStore(d);for(var i=0;i<10;i++)s.AppendDelta(DeltaKind.ConceptChanged,"s",null,new{i});s.TrimDeltas(3);var r=s.ReadDeltas(0);Assert.True(r.Gap);Assert.True(r.Floor>1);Assert.Empty(r.Deltas);}
    [Fact] public void EpochAdvanceIsDurable(){var d=Temp();long e;using(var s=new WorldStore(d)){e=s.AdvanceEpoch("provider_epoch");Assert.Equal(e,s.GetLong("provider_epoch"));}using var s2=new WorldStore(d);Assert.Equal(e,s2.GetLong("provider_epoch"));}
    static string Temp(){var d=Path.Combine(Path.GetTempPath(),"desktopeye-tests",Guid.NewGuid().ToString("N"),"world.db");Directory.CreateDirectory(Path.GetDirectoryName(d)!);return d;}
}