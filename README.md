# Backtesting & Research（BacktestEngine）

**策略回測與市場研究系統｜C# · .NET · WPF · SQLite**

這個專案包含兩個用途不同的子系統。起初建立回測引擎，用來模擬既有交易策略並分析績效；後來發現回測本身不能充分回答「哪些市場條件可能有研究價值」，因此另外發展資料觀測、統計分析與人工審查的研究工作台。

研究目標是尋找值得檢驗的量價特徵與條件，**不代表已證明具有穩定交易優勢**。

## 原始系統的兩個部分

**A. Backtesting Engine（回測引擎）**  
以歷史行情驅動策略、訂單撮合、交易帳務及結果分析。原始專案有獨立 CLI／回測執行流程。

**B. Research Workbench（研究工作台）**  
以另一個 WPF 專案開啟，檢視多週期市場資料、研究案例、觀測證據與驗證狀態。研究資料可得性由 `AvailableAt` 等時間條件控制，避免把未來才知道的資訊誤當成當下證據。

原始完整研究環境依賴未公開的本地市場資料與 SQLite 研究資料庫，本 GitHub **無法重現原始完整系統**。

## 公開版本：兩個可執行的精選流程

> 本 Repository 為 **Isolated Demo**。Synthetic Research Workbench 是另外製作的縮減 WPF 視窗，**不是原始研究工作台介面**；沒有發布原系統截圖或影片。

```text
Synthetic CSV → CLI Replay → Synthetic Teaching Strategy
                                    ↓
                 Original Execution Simulator + Accounting
                                    ↓
                      Report + Synthetic SQLite
                                    ↓
                    Read-only Research Query
                                    ↓
             Original AvailableAt Visibility Policy
                                    ↓
                      Reduced WPF Chart Demo
```

公開回測入口使用新建的合成教學策略，保留實際來源的撮合及會計核心；研究入口則使用合成 SQLite adapter、來源時間可得性規則及縮減後的圖表控制項。公開資料不是真實回測績效，也不是原始研究資料庫的 Schema。

### 程式碼導覽

| 路徑 | 說明 |
|---|---|
| [`src/Core/Source/BacktestEngine.Backtesting/Execution/`](src/Core/Source/BacktestEngine.Backtesting/Execution/) | 來源撮合、費稅、持倉與帳務 |
| [`src/Core/SyntheticReplay.cs`](src/Core/SyntheticReplay.cs) | 新建的合成回放及教學策略 |
| [`src/Cli/Program.cs`](src/Cli/Program.cs) | 精選 CLI 入口 |
| [`src/Core/ResearchStore.cs`](src/Core/ResearchStore.cs) | 新建的合成 SQLite 寫入與唯讀查詢 |
| [`src/Core/ResearchTemporalVisibilityPolicy.cs`](src/Core/ResearchTemporalVisibilityPolicy.cs) | 擷取的 `AvailableAt` 因果時間規則 |
| [`desktop/ResearchChartControl.cs`](desktop/ResearchChartControl.cs) | 來源 WPF 繪圖程式的縮減版本 |
| [`tests/Program.cs`](tests/Program.cs) | 回放、帳務與因果可得時間驗收 |

### 重要工程決策：避免前視偏誤

資料的事件發生時間（`ObservedAt`／Event Time）不一定等於當時實際可取得的時間（`AvailableAt`）。研究查詢使用可得時間與因果角色過濾證據；尚未確認或需要未來資訊的內容，不能被放進過去的決策視角。缺少截至時間時採 Fail Closed，避免誤呈現未來資料。

## 本機執行與驗證

需 .NET SDK 10；WPF 入口需要 Windows。公開版使用合成 CSV，自行產生本機 `research.sqlite`，**不需要原始私人資料庫**。

```powershell
dotnet build src/Cli/BacktestEngine.Cli.csproj
dotnet build desktop/BacktestEngine.Research.Desktop.csproj
dotnet run --project tests/Acceptance.csproj
dotnet run --project src/Cli/BacktestEngine.Cli.csproj -- samples/synthetic.csv artifacts/demo-run
dotnet run --project desktop/BacktestEngine.Research.Desktop.csproj -- artifacts/demo-run/research.sqlite
```

CLI 不覆寫已存在的非空輸出目錄；重跑請換一個輸出目錄。合成驗收案例為 40 筆輸入、2 筆成交、最終部位 0、零費稅模型淨值差額 4,900；這只是固定測試 oracle，**不是策略獲利成果**。

整合驗收涵蓋確定性回放、撮合與帳務核對、訊號同時間不可成交、SQLite 唯讀查詢，以及 `AvailableAt`、未來結果與延遲確認的可見性界線。詳見 [docs/VALIDATION.md](docs/VALIDATION.md)。

## 已知限制（Known Limitations）

未包含私有策略、實際行情、完整原始回測批次、研究資料集、完整 WPF 案例管理、研究治理執行器或正式研究資格驗證。合成 SQLite Schema 不能當成原始 ResearchDataset Schema。原始 .NET 8 程式的公開精選版以 .NET 10 編譯並調整部分相依套件；不能由本切片推論完整原系統的執行狀態。

## 來源與 AI 協作

我主要負責研究問題、需求與架構方向、規格討論及結果驗收；Codex 等 AI 參與技術設計與大量程式實作。公開版本額外使用 AI 建立教學與隔離展示程式，不代表原始兩套子系統全部公開。

來源 Commit：`ee8f0c018fe2f652f0619e2d525806aafd65ecfc`。檔案的來源與改動見 [FILE_ORIGINS.csv](FILE_ORIGINS.csv)、[SOURCE_MANIFEST.json](SOURCE_MANIFEST.json)，公開供閱讀及技術審查，未另行授予開源授權。
