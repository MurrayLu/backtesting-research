using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BacktestEngine.Demo;
namespace BacktestEngine.Research.Desktop;
public static class Program {
 [STAThread]public static void Main(string[] args){
  var dataset=args.Length==1?args[0]:System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory,"../../../../artifacts/demo-run/research.sqlite"));
  if(!System.IO.File.Exists(dataset)){MessageBox.Show("請先執行 CLI 產生合成 research.sqlite，再把檔案路徑傳入研究工作台。");return;}
  var app=new System.Windows.Application();app.Run(new ResearchWindow(dataset));
 }
}
// Reduced WPF host; does not claim to be the full private research workbench.
public sealed class ResearchWindow : Window {
 readonly ResearchStore store;
 readonly ResearchChartControl chart=new();
 readonly TextBlock status=new(){FontSize=21,Foreground=Brushes.White,Margin=new Thickness(0,10,0,15)};
 readonly StackPanel evidence=new();
 readonly long baseUs=new DateTime(2026,10,20,9,0,0).Ticks/10;
 public ResearchWindow(string file){
  store=new ResearchStore(file,true);if(store.Kind()!="SYNTHETIC")throw new InvalidOperationException("Only synthetic datasets are accepted");
  Title="Backtesting & Research — Isolated Demo";Width=1280;Height=820;Background=new SolidColorBrush(Color.FromRgb(9,15,21));Foreground=Brushes.White;WindowStartupLocation=WindowStartupLocation.CenterScreen;
  var root=new DockPanel{Margin=new Thickness(28)};Content=root;
  var header=new StackPanel();DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
  header.Children.Add(new TextBlock{Text="Backtesting & Research / Isolated Demo",FontSize=32,FontWeight=FontWeights.Bold});
  header.Children.Add(new TextBlock{Text="NEW REDUCED UI, NOT ORIGINAL WORKBENCH   •   SYNTHETIC DATA   •   SQLite Read-only   •   Causal evidence only   •   No formal research authorization",FontSize=15,Foreground=Brushes.Turquoise,Margin=new Thickness(0,8,0,8)});
  var buttons=new WrapPanel();header.Children.Add(buttons);
  foreach(var minute in new[]{10,20,35}){var m=minute;var b=new Button{Content=$"截至 09:{minute:00}",Padding=new Thickness(20,9,20,9),Margin=new Thickness(0,5,10,5),FontSize=16};b.Click+=(_,_)=>Show(m);buttons.Children.Add(b);}
  header.Children.Add(status);
  var aside=new Border{Width=300,Padding=new Thickness(20),Background=new SolidColorBrush(Color.FromRgb(21,33,44)),Margin=new Thickness(20,0,0,0),Child=evidence};DockPanel.SetDock(aside,Dock.Right);root.Children.Add(aside);
  var footer=new TextBlock{Text="CLI replay → original execution/accounting → report + synthetic SQLite → read-only query → original AvailableAt policy → adapted original WPF candle renderer",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.SlateGray,FontSize=13,Margin=new Thickness(0,14,0,0)};DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);root.Children.Add(chart);
  Show(20);Closed+=(_,_)=>store.Dispose();
 }
 void Show(int minute){var asOf=baseUs+minute*60_000_000L;var bars=store.Bars(asOf);var rows=store.Evidence(asOf);chart.SetData(bars);
  status.Text=$"SYNTH-A  ·  2026-10-20  ·  截至 09:{minute:00}  ·  可見 {bars.Count} 根 K 線 / {rows.Count} 筆當時證據";
  evidence.Children.Clear();evidence.Children.Add(new TextBlock{Text="當時已知證據",FontSize=22,FontWeight=FontWeights.Bold,Margin=new Thickness(0,0,0,18)});
  foreach(var row in rows)evidence.Children.Add(new TextBlock{Text="● "+row.Label,TextWrapping=TextWrapping.Wrap,FontSize=17,Foreground=Brushes.Turquoise,Margin=new Thickness(0,0,0,18)});
  evidence.Children.Add(new TextBlock{Text="已排除\n延遲確認（Delayed Confirmation）\n未來結果（Forward Outcome）\nAvailableAt 晚於截至時間的證據",TextWrapping=TextWrapping.Wrap,FontSize=15,Foreground=Brushes.LightSlateGray,Margin=new Thickness(0,18,0,0)});
 }
}
