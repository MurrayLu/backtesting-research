using System.Globalization;
using System.Text.Json;
using BacktestEngine.Contracts.Common;
using BacktestEngine.Contracts.Signals;
using BacktestEngine.Backtesting.Execution.Pipeline;
using BacktestEngine.Backtesting.Execution.Fills;
using BacktestEngine.Backtesting.Execution.Accounting;
using BacktestEngine.Backtesting.Execution.Costs;
using BacktestEngine.Backtesting.Execution.Records;
using BacktestEngine.Research.Contracts;
namespace BacktestEngine.Demo;
public sealed record SyntheticTick(long TimestampUs,int PriceE4);
public sealed record ReplayReport(string DataKind,int InputTicks,int FilledOrders,long NetPnlTwd,int FinalPosition,IReadOnlyList<SimulatedTradeRecord> Trades);
// New teaching strategy / CLI orchestration; execution, accounting and contracts below are original source.
public static class SyntheticReplay {
 public static IReadOnlyList<SyntheticTick> Read(string path){
  var rows=File.ReadLines(path).Skip(1).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(line=>{
   var parts=line.Split(',');return new SyntheticTick(long.Parse(parts[0],CultureInfo.InvariantCulture),int.Parse(parts[1],CultureInfo.InvariantCulture));
  }).ToArray();
  if(rows.Length<30 || rows.Any(x=>x.TimestampUs<=0 || x.PriceE4<=0))throw new InvalidDataException("Invalid synthetic replay input");
  for(var i=1;i<rows.Length;i++)if(rows[i].TimestampUs<=rows[i-1].TimestampUs)throw new InvalidDataException("Replay requires strictly increasing timestamps");
  return rows;
 }
 public static StrategyIntent Intent(SyntheticTick tick,bool exit=false)=>new(){
  Sequence=exit?2:1,StrategyId="SyntheticTeachingSchedule",StrategyRunId="synthetic-only",Symbol="SYNTH-A",TradingDate=new(2026,10,20),
  Kind=exit?StrategyIntentKind.Exit:StrategyIntentKind.Entry,Side=exit?StrategyIntentSide.Sell:StrategyIntentSide.Buy,Lots=1,
  Code=StrategyIntentCode.Create(exit?2:1,exit?"SYNTH_EXIT":"SYNTH_ENTRY"),
  TriggerTimestampUs=tick.TimestampUs,DueTimestampUs=tick.TimestampUs,TriggerPriceE4=tick.PriceE4,DecisionPriceE4=tick.PriceE4
 };
 public static MarketSnapshot Snapshot(SyntheticTick tick)=>new(){SymbolId=1,TimestampUs=tick.TimestampUs,LatestPriceE4=tick.PriceE4,HighPriceE4=tick.PriceE4,LowPriceE4=tick.PriceE4,BidPriceE4=tick.PriceE4-1000,AskPriceE4=tick.PriceE4+1000};
 public static SimulatedTradePipeline Pipeline()=>new(new ExecutionSimulator(),new SimulatedAccountingEngine(new FeeTaxSpec{BaseCommissionRate=0,BrokerFeeDiscount=0,MinimumCommissionTwd=0,StockSellTaxRate=0,IntradayStockSellTaxRate=0}));
 public static ReplayReport Run(IReadOnlyList<SyntheticTick> ticks){
  var pipeline=Pipeline();StrategyIntent? pending=null;List<SimulatedTradeRecord> trades=[];
  for(var i=0;i<ticks.Count;i++){
   if(i==2 || i==25)pending=Intent(ticks[i],i==25);
   if(pending is null)continue;
   var result=pipeline.TryProcessIntent(pending,Snapshot(ticks[i]));
   if(result.IsFilled){trades.Add(result.Trade!);pending=null;}
  }
  if(pending is not null)throw new InvalidDataException("Synthetic run ended with pending order");
  return new("SYNTHETIC / NON-INVESTMENT",ticks.Count,trades.Count,trades.Sum(x=>x.NetPnlTwd),pipeline.GetPosition("SYNTH-A",new(2026,10,20)).PositionLots,trades);
 }
 public static IReadOnlyList<ResearchBarRecord> Bars(IReadOnlyList<SyntheticTick> ticks)=>ticks.Select(x=>new ResearchBarRecord(new(2026,10,20),"SYNTH-A","1m",x.TimestampUs-60_000_000,x.TimestampUs,x.PriceE4-1000,x.PriceE4+3000,x.PriceE4-3000,x.PriceE4,100+(x.PriceE4%17)*10,null,null,null,null,null,AvailableAtUs:x.TimestampUs)).ToArray();
 public static string Json(ReplayReport report)=>JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true});
}
