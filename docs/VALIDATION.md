# 發布驗證（Validation）

驗證日期：2026-10-20；Windows、Node.js 24.14.0 / .NET SDK 10。下列命令全部 exit 0。

```powershell
dotnet build desktop/BacktestEngine.Research.Desktop.csproj --nologo
dotnet build src/Cli/BacktestEngine.Cli.csproj --nologo
dotnet run --project tests/Acceptance.csproj
dotnet run --project src/Cli/BacktestEngine.Cli.csproj -- samples/synthetic.csv artifacts/publication-run
dotnet list src/Core/BacktestEngine.Demo.Core.csproj package --vulnerable --include-transitive
```

4 組情境通過：回放報表位元一致與會計對帳、同 timestamp 不成交、SQLite writer 到唯讀 reader 與原始可得時間政策、缺少時間／精確 AvailableAt 邊界。CLI 實際執行：40 ticks、2 fills、final position 0、合成淨額 4900（零費稅教學 oracle，非績效）。CLI 與 WPF 編譯成功，0 errors；套件掃描未回報已知漏洞。未重新執行桌面互動驗收，沒有原始 WPF 畫面證據；不代表完整研究治理驗收。

公開桌面為新建隔離展示（Isolated Demo），不是原始系統畫面。本次不發布截圖、影片或原始正式環境資料。
