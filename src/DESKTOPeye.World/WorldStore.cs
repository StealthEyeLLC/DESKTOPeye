using Microsoft.Data.Sqlite;
using System.Text.Json;
using DESKTOPeye.Protocol;

namespace DESKTOPeye.World;

public sealed record WorldBinding(string ConceptId,string Provider,long ProviderEpoch,string Scope,JsonElement Witness,bool Available);
public sealed record WorldRelation(string SourceId,string Kind,string TargetId,long UpdatedSequence);
public sealed record WorldInterest(string ConceptId,string Reason,JsonElement Projection,long UpdatedSequence);

public sealed class WorldStore : IDisposable
{
    readonly SqliteConnection _db;
    readonly object _gate = new();
    public string Path { get; }
    public WorldStore(string path)
    {
        Path=path; Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        _db=new SqliteConnection($"Data Source={path};Mode=ReadWriteCreate;Cache=Shared;Pooling=True"); _db.Open();
        using var pragma=_db.CreateCommand(); pragma.CommandText="PRAGMA journal_mode=WAL; PRAGMA synchronous=FULL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;"; pragma.ExecuteNonQuery();
        InitializeSchema();
    }
    void InitializeSchema()
    {
        using(var c=_db.CreateCommand())
        {
            c.CommandText=@"
CREATE TABLE IF NOT EXISTS meta(key TEXT PRIMARY KEY,value TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS concepts(id TEXT PRIMARY KEY,kind TEXT NOT NULL,identity TEXT NOT NULL,state INTEGER NOT NULL DEFAULT 0,parent_id TEXT,appinst_id TEXT,stable_key TEXT,created_seq INTEGER NOT NULL,updated_seq INTEGER NOT NULL,retired_seq INTEGER,payload_json TEXT NOT NULL,evidence_json TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS ix_concepts_kind ON concepts(kind);
CREATE INDEX IF NOT EXISTS ix_concepts_parent ON concepts(parent_id);
CREATE INDEX IF NOT EXISTS ix_concepts_key ON concepts(stable_key);
CREATE TABLE IF NOT EXISTS bindings(concept_id TEXT NOT NULL,provider TEXT NOT NULL,provider_epoch INTEGER NOT NULL,scope TEXT NOT NULL,witness_json TEXT NOT NULL,available INTEGER NOT NULL,PRIMARY KEY(concept_id,provider),FOREIGN KEY(concept_id) REFERENCES concepts(id) ON DELETE CASCADE);
CREATE TABLE IF NOT EXISTS relations(source_id TEXT NOT NULL,kind TEXT NOT NULL,target_id TEXT NOT NULL,updated_seq INTEGER NOT NULL,PRIMARY KEY(source_id,kind,target_id));
CREATE TABLE IF NOT EXISTS interests(concept_id TEXT PRIMARY KEY,reason TEXT NOT NULL,projection_json TEXT NOT NULL,updated_seq INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS deltas(seq INTEGER PRIMARY KEY,kind TEXT NOT NULL,scope TEXT NOT NULL,concept_id TEXT,payload_json TEXT NOT NULL,at_utc TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS cursors(name TEXT PRIMARY KEY,seq INTEGER NOT NULL,gap INTEGER NOT NULL DEFAULT 0,updated_utc TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS provider_health(provider TEXT PRIMARY KEY,epoch INTEGER NOT NULL,health TEXT NOT NULL,detail TEXT NOT NULL,updated_seq INTEGER NOT NULL);
INSERT OR IGNORE INTO meta(key,value) VALUES('world_sequence','0'),('delta_floor','1'),('kernel_epoch','0'),('provider_epoch','0'),('session_epoch','1'),('desktop_epoch','1'),('display_epoch','1');";
            c.ExecuteNonQuery();
        }
        EnsureColumn("concepts","state","INTEGER NOT NULL DEFAULT 0");
    }
    void EnsureColumn(string table,string column,string declaration)
    {
        using var q=_db.CreateCommand(); q.CommandText=$"PRAGMA table_info({table})"; using var r=q.ExecuteReader(); bool found=false; while(r.Read()) if(string.Equals(r.GetString(1),column,StringComparison.OrdinalIgnoreCase)){found=true;break;} r.Close();
        if(!found){using var c=_db.CreateCommand();c.CommandText=$"ALTER TABLE {table} ADD COLUMN {column} {declaration}";c.ExecuteNonQuery();}
    }
    public string JournalMode { get { lock(_gate){using var c=_db.CreateCommand(); c.CommandText="PRAGMA journal_mode"; return (string)c.ExecuteScalar()!;} } }
    public string QuickCheck { get { lock(_gate){using var c=_db.CreateCommand(); c.CommandText="PRAGMA quick_check"; return (string)c.ExecuteScalar()!;} } }
    public long GetLong(string key,long fallback=0){ var s=GetMeta(key); return long.TryParse(s,out var v)?v:fallback; }
    public string? GetMeta(string key){ lock(_gate){ using var c=_db.CreateCommand(); c.CommandText="SELECT value FROM meta WHERE key=$k"; c.Parameters.AddWithValue("$k",key); return c.ExecuteScalar() as string; } }
    public void SetMeta(string key,string value){ lock(_gate){ using var c=_db.CreateCommand(); c.CommandText="INSERT INTO meta(key,value) VALUES($k,$v) ON CONFLICT(key) DO UPDATE SET value=excluded.value"; c.Parameters.AddWithValue("$k",key); c.Parameters.AddWithValue("$v",value); c.ExecuteNonQuery(); } }
    public long AdvanceEpoch(string key){ lock(_gate){ using var tx=_db.BeginTransaction(); long cur=GetLongTx(key,tx); long next=cur+1; SetMetaTx(key,next.ToString(),tx); tx.Commit(); return next; } }
    long GetLongTx(string key,SqliteTransaction tx){ using var c=_db.CreateCommand(); c.Transaction=tx; c.CommandText="SELECT value FROM meta WHERE key=$k"; c.Parameters.AddWithValue("$k",key); var s=c.ExecuteScalar() as string; return long.TryParse(s,out var v)?v:0; }
    void SetMetaTx(string key,string value,SqliteTransaction tx){ using var c=_db.CreateCommand(); c.Transaction=tx; c.CommandText="INSERT INTO meta(key,value) VALUES($k,$v) ON CONFLICT(key) DO UPDATE SET value=excluded.value"; c.Parameters.AddWithValue("$k",key); c.Parameters.AddWithValue("$v",value); c.ExecuteNonQuery(); }
    long NextSequenceTx(SqliteTransaction tx){ var n=GetLongTx("world_sequence",tx)+1; SetMetaTx("world_sequence",n.ToString(),tx); return n; }
    public long Head => GetLong("world_sequence");
    public long DeltaFloor => GetLong("delta_floor",1);

    public LogicalConcept UpsertConcept(LogicalConcept concept, DeltaKind kind, string scope, object? deltaPayload=null)
    {
        lock(_gate)
        {
            using var tx=_db.BeginTransaction(); var seq=NextSequenceTx(tx); var c2=concept with { UpdatedSequence=seq, CreatedSequence=concept.CreatedSequence==0?seq:concept.CreatedSequence };
            using(var c=_db.CreateCommand())
            {
                c.Transaction=tx; c.CommandText=@"INSERT INTO concepts(id,kind,identity,state,parent_id,appinst_id,stable_key,created_seq,updated_seq,retired_seq,payload_json,evidence_json) VALUES($id,$k,$i,$st,$p,$a,$s,$c,$u,$r,$j,$e) ON CONFLICT(id) DO UPDATE SET identity=excluded.identity,state=excluded.state,parent_id=excluded.parent_id,appinst_id=excluded.appinst_id,stable_key=excluded.stable_key,updated_seq=excluded.updated_seq,retired_seq=excluded.retired_seq,payload_json=excluded.payload_json,evidence_json=excluded.evidence_json";
                c.Parameters.AddWithValue("$id",c2.Id); c.Parameters.AddWithValue("$k",c2.Kind.ToString()); c.Parameters.AddWithValue("$i",c2.Identity.ToString()); c.Parameters.AddWithValue("$st",(long)c2.State); c.Parameters.AddWithValue("$p",(object?)c2.ParentId??DBNull.Value); c.Parameters.AddWithValue("$a",(object?)c2.AppInstanceId??DBNull.Value); c.Parameters.AddWithValue("$s",(object?)c2.StableKey??DBNull.Value); c.Parameters.AddWithValue("$c",c2.CreatedSequence); c.Parameters.AddWithValue("$u",seq); c.Parameters.AddWithValue("$r",(object?)c2.RetiredSequence??DBNull.Value); c.Parameters.AddWithValue("$j",c2.Properties.GetRawText()); c.Parameters.AddWithValue("$e",JsonSerializer.Serialize(c2.Evidence,JsonDefaults.Options)); c.ExecuteNonQuery();
            }
            InsertDeltaTx(tx,new Delta(seq,kind,scope,c2.Id,JsonDefaults.Element(deltaPayload??c2),DateTimeOffset.UtcNow)); tx.Commit(); return c2;
        }
    }
    public LogicalConcept? GetConcept(string id)
    {
        lock(_gate){using var c=_db.CreateCommand(); c.CommandText=ConceptSelect+" WHERE id=$id"; c.Parameters.AddWithValue("$id",id); using var r=c.ExecuteReader(); return r.Read()?ReadConcept(r):null;}
    }
    public IReadOnlyList<LogicalConcept> ListConcepts(ConceptKind? kind=null,string? parent=null,bool includeRetired=true,string? stableKey=null)
    {
        lock(_gate)
        {
            using var c=_db.CreateCommand(); var where=new List<string>();
            if(kind!=null){where.Add("kind=$k");c.Parameters.AddWithValue("$k",kind.ToString());}
            if(parent!=null){where.Add("parent_id=$p");c.Parameters.AddWithValue("$p",parent);}
            if(!includeRetired)where.Add("retired_seq IS NULL");
            if(stableKey!=null){where.Add("stable_key=$sk");c.Parameters.AddWithValue("$sk",stableKey);}
            c.CommandText=ConceptSelect+(where.Count>0?" WHERE "+string.Join(" AND ",where):"")+" ORDER BY created_seq"; using var r=c.ExecuteReader(); var list=new List<LogicalConcept>(); while(r.Read())list.Add(ReadConcept(r)); return list;
        }
    }
    const string ConceptSelect="SELECT id,kind,identity,state,parent_id,appinst_id,stable_key,created_seq,updated_seq,retired_seq,payload_json,evidence_json FROM concepts";
    static LogicalConcept ReadConcept(SqliteDataReader r)
    {
        var props=JsonDocument.Parse(r.GetString(10)).RootElement.Clone(); var ev=JsonSerializer.Deserialize<IdentityEvidence>(r.GetString(11),JsonDefaults.Options)!;
        return new LogicalConcept(r.GetString(0),Enum.Parse<ConceptKind>(r.GetString(1)),Enum.Parse<IdentityStatus>(r.GetString(2)),(UiState)r.GetInt64(3),r.IsDBNull(4)?null:r.GetString(4),r.IsDBNull(5)?null:r.GetString(5),r.IsDBNull(6)?null:r.GetString(6),r.GetInt64(7),r.GetInt64(8),r.IsDBNull(9)?null:r.GetInt64(9),props,ev);
    }
    public LogicalConcept Retire(string id,IdentityStatus status,string reason,string scope)
    {
        var c=GetConcept(id)??throw new KeyNotFoundException(id); var seq=Head+1; var next=c with{Identity=status,RetiredSequence=seq,Evidence=c.Evidence with{Note=reason}}; return UpsertConcept(next,DeltaKind.ConceptRetired,scope,new{status,reason});
    }

    public void UpsertBinding(string conceptId,string provider,long providerEpoch,string scope,object witness,bool available=true)
    {
        lock(_gate){using var c=_db.CreateCommand();c.CommandText="INSERT INTO bindings(concept_id,provider,provider_epoch,scope,witness_json,available) VALUES($id,$p,$e,$s,$w,$a) ON CONFLICT(concept_id,provider) DO UPDATE SET provider_epoch=excluded.provider_epoch,scope=excluded.scope,witness_json=excluded.witness_json,available=excluded.available";c.Parameters.AddWithValue("$id",conceptId);c.Parameters.AddWithValue("$p",provider);c.Parameters.AddWithValue("$e",providerEpoch);c.Parameters.AddWithValue("$s",scope);c.Parameters.AddWithValue("$w",JsonSerializer.Serialize(witness,JsonDefaults.Options));c.Parameters.AddWithValue("$a",available?1:0);c.ExecuteNonQuery();}
    }
    public IReadOnlyList<WorldBinding> GetBindings(string conceptId)
    {
        lock(_gate){using var c=_db.CreateCommand();c.CommandText="SELECT concept_id,provider,provider_epoch,scope,witness_json,available FROM bindings WHERE concept_id=$id ORDER BY provider";c.Parameters.AddWithValue("$id",conceptId);using var r=c.ExecuteReader();var l=new List<WorldBinding>();while(r.Read())l.Add(new(r.GetString(0),r.GetString(1),r.GetInt64(2),r.GetString(3),JsonDocument.Parse(r.GetString(4)).RootElement.Clone(),r.GetInt64(5)!=0));return l;}
    }
    public WorldBinding? GetBinding(string conceptId,string provider)=>GetBindings(conceptId).FirstOrDefault(x=>x.Provider==provider);
    public void SetBindingAvailable(string conceptId,string provider,bool available){lock(_gate){using var c=_db.CreateCommand();c.CommandText="UPDATE bindings SET available=$a WHERE concept_id=$id AND provider=$p";c.Parameters.AddWithValue("$a",available?1:0);c.Parameters.AddWithValue("$id",conceptId);c.Parameters.AddWithValue("$p",provider);c.ExecuteNonQuery();}}
    public void RemoveBinding(string conceptId,string provider){lock(_gate){using var c=_db.CreateCommand();c.CommandText="DELETE FROM bindings WHERE concept_id=$id AND provider=$p";c.Parameters.AddWithValue("$id",conceptId);c.Parameters.AddWithValue("$p",provider);c.ExecuteNonQuery();}}

    public void UpsertRelation(string source,string kind,string target)
    {
        lock(_gate){using var tx=_db.BeginTransaction();var seq=NextSequenceTx(tx);using var c=_db.CreateCommand();c.Transaction=tx;c.CommandText="INSERT INTO relations(source_id,kind,target_id,updated_seq) VALUES($s,$k,$t,$u) ON CONFLICT(source_id,kind,target_id) DO UPDATE SET updated_seq=excluded.updated_seq";c.Parameters.AddWithValue("$s",source);c.Parameters.AddWithValue("$k",kind);c.Parameters.AddWithValue("$t",target);c.Parameters.AddWithValue("$u",seq);c.ExecuteNonQuery();InsertDeltaTx(tx,new Delta(seq,DeltaKind.ConceptChanged,"relation",source,JsonDefaults.Element(new{kind,target}),DateTimeOffset.UtcNow));tx.Commit();}
    }
    public IReadOnlyList<WorldRelation> GetRelations(string source,string? kind=null)
    {
        lock(_gate){using var c=_db.CreateCommand();c.CommandText="SELECT source_id,kind,target_id,updated_seq FROM relations WHERE source_id=$s"+(kind is null?"":" AND kind=$k")+" ORDER BY kind,target_id";c.Parameters.AddWithValue("$s",source);if(kind!=null)c.Parameters.AddWithValue("$k",kind);using var r=c.ExecuteReader();var l=new List<WorldRelation>();while(r.Read())l.Add(new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetInt64(3)));return l;}
    }
    public void RemoveRelations(string source,string? kind=null){lock(_gate){using var c=_db.CreateCommand();c.CommandText="DELETE FROM relations WHERE source_id=$s"+(kind is null?"":" AND kind=$k");c.Parameters.AddWithValue("$s",source);if(kind!=null)c.Parameters.AddWithValue("$k",kind);c.ExecuteNonQuery();}}

    public void UpsertInterest(string conceptId,string reason,object projection)
    {
        lock(_gate){var seq=Head;using var c=_db.CreateCommand();c.CommandText="INSERT INTO interests(concept_id,reason,projection_json,updated_seq) VALUES($id,$r,$p,$u) ON CONFLICT(concept_id) DO UPDATE SET reason=excluded.reason,projection_json=excluded.projection_json,updated_seq=excluded.updated_seq";c.Parameters.AddWithValue("$id",conceptId);c.Parameters.AddWithValue("$r",reason);c.Parameters.AddWithValue("$p",JsonSerializer.Serialize(projection,JsonDefaults.Options));c.Parameters.AddWithValue("$u",seq);c.ExecuteNonQuery();}
    }
    public IReadOnlyList<WorldInterest> ListInterests()
    {
        lock(_gate){using var c=_db.CreateCommand();c.CommandText="SELECT concept_id,reason,projection_json,updated_seq FROM interests ORDER BY concept_id";using var r=c.ExecuteReader();var l=new List<WorldInterest>();while(r.Read())l.Add(new(r.GetString(0),r.GetString(1),JsonDocument.Parse(r.GetString(2)).RootElement.Clone(),r.GetInt64(3)));return l;}
    }
    public void RemoveInterest(string conceptId){lock(_gate){using var c=_db.CreateCommand();c.CommandText="DELETE FROM interests WHERE concept_id=$id";c.Parameters.AddWithValue("$id",conceptId);c.ExecuteNonQuery();}}

    void InsertDeltaTx(SqliteTransaction tx,Delta d){using var c=_db.CreateCommand(); c.Transaction=tx; c.CommandText="INSERT INTO deltas(seq,kind,scope,concept_id,payload_json,at_utc) VALUES($s,$k,$sc,$id,$p,$a)"; c.Parameters.AddWithValue("$s",d.Sequence); c.Parameters.AddWithValue("$k",d.Kind.ToString()); c.Parameters.AddWithValue("$sc",d.Scope); c.Parameters.AddWithValue("$id",(object?)d.ConceptId??DBNull.Value); c.Parameters.AddWithValue("$p",d.Payload.GetRawText()); c.Parameters.AddWithValue("$a",d.At.UtcDateTime.ToString("O")); c.ExecuteNonQuery();}
    public long AppendDelta(DeltaKind kind,string scope,string? conceptId,object payload){lock(_gate){using var tx=_db.BeginTransaction(); var seq=NextSequenceTx(tx); InsertDeltaTx(tx,new Delta(seq,kind,scope,conceptId,JsonDefaults.Element(payload),DateTimeOffset.UtcNow)); tx.Commit(); return seq;}}
    public DeltaRead ReadDeltas(long cursor,int max=256,string? scope=null)
    {
        lock(_gate)
        {
            var floor=DeltaFloor; var head=Head; if(cursor<floor-1)return new DeltaRead(cursor,floor,head,true,Array.Empty<Delta>(),scope is null?new[]{"*"}:new[]{scope});
            using var c=_db.CreateCommand(); c.CommandText="SELECT seq,kind,scope,concept_id,payload_json,at_utc FROM deltas WHERE seq>$cur"+(scope is null?"":" AND scope=$scope")+" ORDER BY seq LIMIT $m"; c.Parameters.AddWithValue("$cur",cursor); c.Parameters.AddWithValue("$m",max); if(scope is not null)c.Parameters.AddWithValue("$scope",scope); using var r=c.ExecuteReader(); var list=new List<Delta>(); while(r.Read())list.Add(new Delta(r.GetInt64(0),Enum.Parse<DeltaKind>(r.GetString(1)),r.GetString(2),r.IsDBNull(3)?null:r.GetString(3),JsonDocument.Parse(r.GetString(4)).RootElement.Clone(),DateTimeOffset.Parse(r.GetString(5)))); return new DeltaRead(cursor,floor,head,false,list.ToArray(),Array.Empty<string>());
        }
    }
    public long MarkGap(string reason,string[] scopes)=>AppendDelta(DeltaKind.Gap,"*",null,new{reason,scopes});
    public void TrimDeltas(int retainCount){lock(_gate){var head=Head;var newFloor=Math.Max(1,head-retainCount+1);using var tx=_db.BeginTransaction();using(var c=_db.CreateCommand()){c.Transaction=tx;c.CommandText="DELETE FROM deltas WHERE seq<$f";c.Parameters.AddWithValue("$f",newFloor);c.ExecuteNonQuery();}SetMetaTx("delta_floor",newFloor.ToString(),tx);tx.Commit();}}
    public void SetProviderHealth(string provider,long epoch,ProviderHealth health,string detail)
    {
        lock(_gate){using var tx=_db.BeginTransaction();var seq=NextSequenceTx(tx);using(var c=_db.CreateCommand()){c.Transaction=tx;c.CommandText="INSERT INTO provider_health(provider,epoch,health,detail,updated_seq) VALUES($p,$e,$h,$d,$s) ON CONFLICT(provider) DO UPDATE SET epoch=excluded.epoch,health=excluded.health,detail=excluded.detail,updated_seq=excluded.updated_seq";c.Parameters.AddWithValue("$p",provider);c.Parameters.AddWithValue("$e",epoch);c.Parameters.AddWithValue("$h",health.ToString());c.Parameters.AddWithValue("$d",detail);c.Parameters.AddWithValue("$s",seq);c.ExecuteNonQuery();}InsertDeltaTx(tx,new Delta(seq,DeltaKind.ProviderHealthChanged,"provider",null,JsonDefaults.Element(new{provider,epoch,health,detail}),DateTimeOffset.UtcNow));tx.Commit();}
    }
    public void Dispose()=>_db.Dispose();
}