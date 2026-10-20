using System.Text.Json;
using Microsoft.Data.Sqlite;
using BacktestEngine.Research.Contracts;
using BacktestEngine.Research.Application;
namespace BacktestEngine.Demo;
public sealed record EvidenceRow(string Id,string Label,ResearchTemporalEnvelope Temporal);
// New synthetic-only dataset adapter. The temporal visibility policy is extracted original source.
public sealed class ResearchStore : IDisposable {
 readonly SqliteConnection db;
 public ResearchStore(string path,bool readOnly=false){db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Mode=readOnly?SqliteOpenMode.ReadOnly:SqliteOpenMode.ReadWriteCreate}.ToString());db.Open();}
 public void Create(IReadOnlyList<ResearchBarRecord> bars){
  using var tx=db.BeginTransaction();
  using var cmd=db.CreateCommand();cmd.Transaction=tx;
  cmd.CommandText="CREATE TABLE bars(at INTEGER PRIMARY KEY, payload TEXT NOT NULL);CREATE TABLE evidence(id TEXT PRIMARY KEY,payload TEXT NOT NULL);CREATE TABLE metadata(kind TEXT NOT NULL);INSERT INTO metadata VALUES ('SYNTHETIC');";cmd.ExecuteNonQuery();
  foreach(var bar in bars){cmd.CommandText="INSERT INTO bars VALUES($at,$payload)";cmd.Parameters.Clear();cmd.Parameters.AddWithValue("$at",bar.CloseTimestampUs);cmd.Parameters.AddWithValue("$payload",JsonSerializer.Serialize(bar));cmd.ExecuteNonQuery();}
  var evidence=new[]{
   new EvidenceRow("known","已可取得的當時證據",new(bars[5].CloseTimestampUs,bars[5].CloseTimestampUs,null,ResearchTemporalRole.EventTimeFeature,ResearchTemporalMode.Causal)),
   new EvidenceRow("later","09:15 才可取得的合成證據",new(bars[10].CloseTimestampUs,bars[15].CloseTimestampUs,null,ResearchTemporalRole.EventTimeFeature,ResearchTemporalMode.Causal)),
   new EvidenceRow("delayed","延遲確認：不可當成即時特徵",new(bars[5].CloseTimestampUs,bars[15].CloseTimestampUs,null,ResearchTemporalRole.EventTimeFeature,ResearchTemporalMode.DelayedConfirmation)),
   new EvidenceRow("outcome","未來結果：不可當成當時證據",new(bars[5].CloseTimestampUs,bars[20].CloseTimestampUs,null,ResearchTemporalRole.ForwardOutcome,ResearchTemporalMode.Retrospective))};
  foreach(var row in evidence){cmd.CommandText="INSERT INTO evidence VALUES($id,$payload)";cmd.Parameters.Clear();cmd.Parameters.AddWithValue("$id",row.Id);cmd.Parameters.AddWithValue("$payload",JsonSerializer.Serialize(row));cmd.ExecuteNonQuery();}
  tx.Commit();
 }
 public IReadOnlyList<ResearchBarRecord> Bars(long asOf){
  using var cmd=db.CreateCommand();cmd.CommandText="SELECT payload FROM bars WHERE at<=$asOf ORDER BY at";cmd.Parameters.AddWithValue("$asOf",asOf);
  using var r=cmd.ExecuteReader();List<ResearchBarRecord> rows=[];while(r.Read())rows.Add(JsonSerializer.Deserialize<ResearchBarRecord>(r.GetString(0))!);return rows;
 }
 public IReadOnlyList<EvidenceRow> Evidence(long asOf){
  using var cmd=db.CreateCommand();cmd.CommandText="SELECT payload FROM evidence ORDER BY id";using var r=cmd.ExecuteReader();List<EvidenceRow> rows=[];
  while(r.Read()){var row=JsonSerializer.Deserialize<EvidenceRow>(r.GetString(0))!;if(ResearchTemporalVisibilityPolicy.IsVisible(row.Temporal,new(CausalOnly:true,AsOfTimestampUs:asOf)))rows.Add(row);}return rows;
 }
 public string Kind(){using var cmd=db.CreateCommand();cmd.CommandText="SELECT kind FROM metadata";return (string)cmd.ExecuteScalar()!;}
 public void Dispose()=>db.Dispose();
}
