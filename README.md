# Backtesting & Research（BacktestEngine）

**回測引擎與研究工作台（Backtesting Engine & Research Workbench）**

> 公開程式為隔離展示（Isolated Demo）。Synthetic Research Workbench 是本次新建縮減視窗，不是原始研究工作台介面。本次不公開任何截圖或影片。

兩個不同啟動入口：CLI 將合成行情送入真正的交易模擬與會計主線，輸出報告和研究 SQLite；WPF 工作台只讀取研究資料，依可得時間顯示 K 線與當時證據。這是個人研究工程展示，不宣稱策略獲利或正式研究權威已核准。


## A. 回測引擎（Backtesting Engine）

入口：`src/Cli/BacktestEngine.Cli.csproj`。讀取明示合成的 CSV，依新建教學排程產生策略意圖（Strategy Intent），交給原始 `ExecutionSimulator` 與 `SimulatedAccountingEngine`，核對成交、持倉與報表。教學排程不是原始專有策略；合成費稅設定為零，以便手算對帳，不能作投資結果。

## B. 研究工作台（Research Workbench）

入口：`desktop/BacktestEngine.Research.Desktop.csproj`。以唯讀 SQLite 查詢 K 線與證據，使用原始 `ResearchTemporalVisibilityPolicy` 篩選當時可得資訊。WPF K 線／量柱 renderer 由原始 `ResearchChartControl` 的 palette 與 DrawMain 繪圖迴圈縮減調整而來；安全視窗與資料 adapter 是本次新建，不是私有完整 MainWindow，也不會在 UI 內執行原始全套研究或策略。

## 系統架構（Architecture）

```text
Synthetic CSV → CLI replay → new teaching strategy events
                              ↓ original intent / execution / accounting
                         report.json + synthetic research.sqlite
                                           ↓ read-only SQLite query
                             original AvailableAt visibility policy
                                           ↓
                              adapted original WPF candle renderer
```

## 本機操作（Local Reproduction）

需要 .NET SDK 10；WPF 入口需要 Windows。

```powershell
dotnet build src/Cli/BacktestEngine.Cli.csproj
dotnet build desktop/BacktestEngine.Research.Desktop.csproj
dotnet run --project tests/Acceptance.csproj
dotnet run --project src/Cli/BacktestEngine.Cli.csproj -- samples/synthetic.csv artifacts/demo-run
dotnet run --project desktop/BacktestEngine.Research.Desktop.csproj -- artifacts/demo-run/research.sqlite
```

CLI 拒絕覆寫非空輸出目錄；重跑請改用新的輸出目錄。已知合成結果為 40 筆輸入、2 筆成交、最終部位 0，零費稅教學模型的淨值差額 4,900，測試會對帳並比對重跑報告完全一致。這個數字僅為合成驗收 oracle。

工作台點「截至 09:10」應顯示 11 根 K 線／1 筆證據；「截至 09:20」為 21 根／2 筆。延遲確認（Delayed Confirmation）與未來結果（Forward Outcome）仍不得變成當時可得證據。

## 程式閱讀導覽（Code Walkthrough）

| 路徑 | 檢查重點 |
|---|---|
| `src/Core/Source/BacktestEngine.Contracts/` | 真實 MarketSnapshot、OrderPlan、StrategyIntent 契約 |
| `src/Core/Source/BacktestEngine.Backtesting/Execution/` | 真正的撮合時間規則、滑價／費稅邊界、會計、持倉與交易紀錄 |
| `src/Core/SyntheticReplay.cs`、`src/Cli/Program.cs` | 新建合成輸入、教學策略、回放與輸出閉環 |
| `src/Core/ResearchStore.cs` | 新建合成資料 SQLite writer 與 read-only reader |
| `src/Core/ResearchTemporalVisibilityPolicy.cs` | 從來源擷取的可得時間判斷；視圖契約縮減，核心判斷保留 |
| `desktop/ResearchChartControl.cs` | 原始 WPF palette／量價繪圖迴圈的可編譯縮減版 |
| `tests/Program.cs` | CLI 與 Research 分別標示的整合／失敗驗收 |

## 技術決策與驗證

保留原始 execution／accounting 主線，僅以教學策略替換敏感策略。成交必須晚於送單可用時間，避免同 timestamp 先後不明。研究資料採只讀連線，AvailableAt 不可被 ObservedAt 取代；缺少截至時間採失敗關閉（Fail Closed）。驗收涵蓋確定性報表、部位／帳務對帳、同時間拒絕成交、SQLite 投影、未來資料隱藏、精確時間邊界及延遲／結果資料排除。

## 本人角色與 AI 協作

本作品集由本人選定公開範圍並核准發布。本輪使用 AI 協助讀碼、擷取、建立隔離展示入口、合成資料、測試與文件；來源原碼、調整碼及新建展示碼分別記錄在來源清單。原始專案的精確分工與歷史 AI 使用情形未在本次查核，因此不主張全部獨立手寫、手寫比例、正式部署或完整原系統驗收。

## 已知限制（Known Limitations）

公開版未包含原始 CLI 完整批次系統、私有策略、實際行情、回測績效、資料集、桌面案例管理及正式研究治理執行器。新建 SQLite schema 是合成展示 adapter，沒有冒充原始 ResearchDataset Schema。切片不能證明完整平台完成資格驗證（Qualification）或正式驗收（Production Acceptance）；未改變任何研究語意發布或權威狀態。原始 .NET 8 程式在候選版以 .NET 10 編譯，依賴亦更新以排除舊 SQLite native 套件公告。


依賴重現性：本候選附 NuGet `packages.lock.json`，預設 locked restore；若修改套件版本，須另行重驗，不可沿用本次證據。


來源 Commit SHA：`ee8f0c018fe2f652f0619e2d525806aafd65ecfc`。原始回測引擎與獨立研究工作台分別取材；公開版僅驗證上述合成切片，並非兩套原系統的完整搬移。
