using BacktestEngine.Demo;
if(args.Length!=2){Console.Error.WriteLine("Usage: BacktestEngine.Cli <synthetic.csv> <new-output-directory>");return 2;}
if(Directory.Exists(args[1]) && Directory.EnumerateFileSystemEntries(args[1]).Any()){Console.Error.WriteLine("Refusing to overwrite existing output directory");return 3;}
var ticks=SyntheticReplay.Read(args[0]);
var report=SyntheticReplay.Run(ticks);
Directory.CreateDirectory(args[1]);
File.WriteAllText(Path.Combine(args[1],"report.json"),SyntheticReplay.Json(report));
using(var store=new ResearchStore(Path.Combine(args[1],"research.sqlite")))store.Create(SyntheticReplay.Bars(ticks));
Console.WriteLine("SYNTHETIC ONLY — teaching schedule, no proprietary strategy");
Console.WriteLine($"Ticks={report.InputTicks}; Fills={report.FilledOrders}; Final position={report.FinalPosition}; Synthetic net={report.NetPnlTwd}");
Console.WriteLine("report.json + research.sqlite generated; no external service accessed.");
return 0;
