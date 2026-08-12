using DESKTOPeye.Protocol;
using DESKTOPeye.World;
using Microsoft.Data.Sqlite;

namespace DESKTOPeye.Unit.Tests;

public class WorldStoreTests
{
    static IdentityEvidence E=>new("test",new(),Array.Empty<string>(),1,1,"unit");
    static LogicalConcept Concept(string id="control_x")=>new(id,ConceptKind.Control,IdentityStatus.exact,UiState.Enabled,"window_x","appinst_x","uia:automationId:PrimaryText",0,0,null,JsonDefaults.Element(new{name="x"}),E);

    [Fact] public void WalAndQuickCheckAreHealthy(){var d=Temp();using var s=new WorldStore(d);Assert.Equal("wal",s.JournalMode,StringComparer.OrdinalIgnoreCase);Assert.Equal("ok",s.QuickCheck);}
    [Fact] public void ConceptCommitAdvancesOneWorldSequence(){var d=Temp();using var s=new WorldStore(d);var x=s.UpsertConcept(Concept(),DeltaKind.ConceptCreated,"test");Assert.Equal(1,x.UpdatedSequence);Assert.Equal(1,s.Head);Assert.Single(s.ReadDeltas(0).Deltas);}
    [Fact] public void ExpiredCursorReturnsExplicitGap(){var d=Temp();using var s=new WorldStore(d);for(var i=0;i<10;i++)s.AppendDelta(DeltaKind.ConceptChanged,"s",null,new{i});s.TrimDeltas(3);var r=s.ReadDeltas(0);Assert.True(r.Gap);Assert.True(r.Floor>1);Assert.Empty(r.Deltas);}
    [Fact] public void EpochAdvanceIsDurable(){var d=Temp();long e;using(var s=new WorldStore(d)){e=s.AdvanceEpoch("provider_epoch");Assert.Equal(e,s.GetLong("provider_epoch"));}using var s2=new WorldStore(d);Assert.Equal(e,s2.GetLong("provider_epoch"));}
    [Fact] public void RetainedStateReopensWithBindingsRelationsInterestsAndDeltas()
    {
        var d=Temp();long head;
        using(var s=new WorldStore(d))
        {
            var c=s.UpsertConcept(Concept(),DeltaKind.ConceptCreated,"test");
            s.UpsertBinding(c.Id,"uia",77,c.ParentId!,new{runtimeId="1.2",providerEpoch=77},true);
            s.UpsertRelation(c.Id,"contains","item_1");
            s.UpsertInterest(c.Id,"acceptance",new{properties=new[]{"value"}});
            s.SetMeta("session_desktop_epoch","9");head=s.Head;
        }
        using var reopened=new WorldStore(d);
        var rc=reopened.GetConcept("control_x");Assert.NotNull(rc);Assert.Equal(IdentityStatus.exact,rc!.Identity);Assert.Equal("uia:automationId:PrimaryText",rc.StableKey);
        Assert.True(reopened.GetBinding(rc.Id,"uia")!.Available);Assert.Single(reopened.GetRelations(rc.Id,"contains"));Assert.Single(reopened.ListInterests());Assert.Equal(9,reopened.GetLong("session_desktop_epoch"));Assert.Equal(head,reopened.Head);Assert.NotEmpty(reopened.ReadDeltas(0).Deltas);Assert.Equal("ok",reopened.QuickCheck);
    }
    [Fact] public void PersistenceSchemaContainsOnlyCapabilityStateNotGenericActionLedger()
    {
        var d=Temp();using(var s=new WorldStore(d)){}
        using var db=new SqliteConnection($"Data Source={d};Mode=ReadOnly");db.Open();using var c=db.CreateCommand();c.CommandText="SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";using var r=c.ExecuteReader();var names=new List<string>();while(r.Read())names.Add(r.GetString(0));
        Assert.Contains("concepts",names);Assert.Contains("bindings",names);Assert.Contains("deltas",names);Assert.DoesNotContain(names,n=>n.Contains("action",StringComparison.OrdinalIgnoreCase)||n.Contains("receipt",StringComparison.OrdinalIgnoreCase)||n.Contains("command",StringComparison.OrdinalIgnoreCase));
    }
    static string Temp(){var d=Path.Combine(Path.GetTempPath(),"desktopeye-tests",Guid.NewGuid().ToString("N"),"world.db");Directory.CreateDirectory(Path.GetDirectoryName(d)!);return d;}
}