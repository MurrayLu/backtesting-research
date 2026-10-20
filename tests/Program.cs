using BacktestEngine.Demo;
using BacktestEngine.Backtesting.Execution.Fills;
using BacktestEngine.Research.Application;
using BacktestEngine.Research.Contracts;
void Check(bool value,string message){if(!value)throw new Exception(message);}
var ticks=SyntheticReplay.Read("samples/synthetic.csv");
var first=SyntheticReplay.Run(ticks);var second=SyntheticReplay.Run(ticks);
Check(SyntheticReplay.Json(first)==SyntheticReplay.Json(second),"deterministic byte-for-byte report");
Check(first.FilledOrders==2 && first.FinalPosition==0 && first.NetPnlTwd==4900,"known synthetic round trip must reconcile to 4900, flat position");
Console.WriteLine("PASS CLI replay → teaching strategy → actual simulator/accounting → deterministic reconciled report");
var simulator=new ExecutionSimulator();var intent=SyntheticReplay.Intent(ticks[2]);
Check(!simulator.TryExecute(intent,SyntheticReplay.Snapshot(ticks[2])).IsFilled,"same-time event must not fill");
Check(simulator.TryExecute(intent,SyntheticReplay.Snapshot(ticks[3])).IsFilled,"later event may fill");
Console.WriteLine("PASS execution temporal boundary rejects same-time fills");
var temp=Path.Combine(Path.GetTempPath(),"portfolio-research-"+Guid.NewGuid()+".sqlite");
try{
 using(var store=new ResearchStore(temp))store.Create(SyntheticReplay.Bars(ticks));
 using(var reader=new ResearchStore(temp,true)){
  Check(reader.Kind()=="SYNTHETIC","dataset must explicitly declare synthetic provenance");
  var early=reader.Evidence(ticks[10].TimestampUs);var late=reader.Evidence(ticks[20].TimestampUs);
  Check(early.Count==1 && early[0].Id=="known","future-available evidence must be hidden");
  Check(late.Count==2 && late.Any(x=>x.Id=="later"),"evidence must appear at legal availability");
  Check(late.All(x=>x.Id is not "delayed" and not "outcome"),"delayed confirmation and outcome cannot become current evidence");
  Check(reader.Bars(ticks[10].TimestampUs).Count==11,"future bars must be absent from reader");
 }
 Console.WriteLine("PASS Research SQLite writer → read-only reader → original causal visibility policy");
 var temporal=new ResearchTemporalEnvelope(10,20,null,ResearchTemporalRole.EventTimeFeature,ResearchTemporalMode.Causal);
 Check(!ResearchTemporalVisibilityPolicy.IsVisible(temporal,new(CausalOnly:true)),"missing asOf must fail closed");
 Check(!ResearchTemporalVisibilityPolicy.IsVisible(temporal,new(CausalOnly:true,AsOfTimestampUs:19)),"before AvailableAt must hide");
 Check(ResearchTemporalVisibilityPolicy.IsVisible(temporal,new(CausalOnly:true,AsOfTimestampUs:20)),"at AvailableAt must show");
 Console.WriteLine("PASS Research missing-time and exact AvailableAt boundary");
}finally{Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();File.Delete(temp);}
Console.WriteLine("Backtest and research acceptance complete.");
