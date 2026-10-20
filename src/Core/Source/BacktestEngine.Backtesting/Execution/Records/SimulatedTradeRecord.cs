namespace BacktestEngine.Backtesting.Execution.Records;

/// <summary>
/// 通用模擬成交紀錄。
///
/// 這是一筆 fill-level record。
/// 也就是：每成交一批，就產生一筆 SimulatedTradeRecord。
///
/// 它不是策略訊號，也不是一整段 round trip。
/// 它代表「某策略在某時間，因某個 trigger，模擬成交了幾張」。
/// </summary>
public sealed class SimulatedTradeRecord
{
    /// <summary>
    /// 成交序號。
    ///
    /// 一次 backtest run 內可遞增，用來穩定排序。
    /// 第一版尚未接進 simulator 時可以先維持 0。
    /// </summary>
    public long Sequence { get; init; }

    /// <summary>
    /// 策略 ID。
    ///
    /// 例如：
    /// - intraday_strength_fade
    /// - intraday_trend
    ///
    /// 這是 fill-level record 的 metadata，不是 tick 熱路徑判斷欄位。
    /// </summary>
    public required string StrategyId { get; init; }

    /// <summary>
    /// 策略執行實例 ID。
    ///
    /// 第一版可以空白。
    /// 未來若同一策略同一天跑多組參數，可以用它區分。
    /// </summary>
    public string StrategyRunId { get; init; } = "";

    /// <summary>
    /// 交易日。
    /// </summary>
    public DateOnly TradingDate { get; init; }

    /// <summary>
    /// 商品代碼。
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// 成交方向。
    /// </summary>
    public SimulatedTradeSide Side { get; init; }

    /// <summary>
    /// 策略意圖大類。
    ///
    /// 只允許通用 Entry / Exit。
    /// 策略內部細分原因請使用 TriggerId / TriggerCode / TriggerGroup。
    /// </summary>
    public SimulatedTradeIntentKind IntentKind { get; init; } =
        SimulatedTradeIntentKind.Unknown;

    /// <summary>
    /// 這筆成交對倉位造成的數學效果。
    /// </summary>
    public SimulatedPositionEffect PositionEffect { get; init; }

    /// <summary>
    /// 策略內部 trigger 數字 ID。
    ///
    /// 這是給程式統計、比對、排序用的高效率欄位。
    /// 不同策略可以自己定義自己的 enum，最後轉成 int 存進這裡。
    ///
    /// 範例：
    /// - trend: 101 = ENTRY_LONG_GENERAL_TREND
    /// - trend: 102 = ENTRY_LONG_EXTREME_TREND
    /// - fade:  101 = ENTRY_SHORT_FADE
    /// </summary>
    public int TriggerId { get; init; }

    /// <summary>
    /// 策略內部 trigger code。
    ///
    /// 這是報表與 AI review 使用的穩定文字代號。
    ///
    /// 範例：
    /// - ENTRY_SHORT_FADE
    /// - ENTRY_LONG_GENERAL_TREND
    /// - ENTRY_LONG_EXTREME_TREND
    /// - EXIT_LONG_HARD_STOP
    /// - EXIT_LONG_EXHAUSTION
    /// - EXIT_SHORT_TIME
    ///
    /// 設計成 string，是為了讓不同策略自由擴充自己的訊號種類。
    /// 熱路徑請使用 TriggerId。
    /// </summary>
    public string TriggerCode { get; init; } = "";

    /// <summary>
    /// trigger 分組。
    ///
    /// 用於報表聚合。
    ///
    /// 範例：
    /// - FADE
    /// - GENERAL_TREND
    /// - EXTREME_TREND
    /// - STOP
    /// - EXHAUSTION
    /// - TIME_EXIT
    /// </summary>
    public string TriggerGroup { get; init; } = "";

    /// <summary>
    /// 人類可讀的 trigger 名稱。
    ///
    /// 範例：
    /// - 強勢轉弱放空進場
    /// - 一般趨勢多單進場
    /// - 極端趨勢多單進場
    /// - 硬停損出場
    /// - 衰竭出場
    /// </summary>
    public string TriggerName { get; init; } = "";

    /// <summary>
    /// 完整觸發原因說明。
    ///
    /// 這欄給報表與 AI review 使用。
    /// 例如可以記錄當下 MA / VWAP / volume / stop price 等條件。
    /// </summary>
    public string TriggerDetail { get; init; } = "";

    public Guid EntryCycleId { get; init; }

    public Guid CorrelationId { get; init; }

    /// <summary>
    /// 觸發訊號的時間。
    ///
    /// 例如策略判定進場訊號成立的 timestamp。
    /// </summary>
    public long TriggerTimestampUs { get; init; }

    /// <summary>
    /// 觸發訊號當下的價格 e4。
    ///
    /// 不一定等於最終成交價。
    /// </summary>
    public int TriggerPriceE4 { get; init; }

    /// <summary>
    /// 預定成交時間。
    ///
    /// 分批成交時，這通常是 NextEntryDueUs / NextExitDueUs。
    /// </summary>
    public long DueTimestampUs { get; init; }

    /// <summary>
    /// 模擬送單時間。
    ///
    /// 未來接 order latency 時：
    /// SubmittedTimestampUs = DueTimestampUs + OrderLatencyUs。
    ///
    /// 第一版尚未接 latency 時可等於 DueTimestampUs。
    /// </summary>
    public long SubmittedTimestampUs { get; init; }

    /// <summary>
    /// 實際模擬成交時間。
    /// </summary>
    public long FillTimestampUs { get; init; }

    /// <summary>
    /// 下單延遲。
    ///
    /// SubmittedTimestampUs - DueTimestampUs。
    /// </summary>
    public long OrderLatencyUs => SubmittedTimestampUs - DueTimestampUs;

    /// <summary>
    /// 送單後等待成交的延遲。
    ///
    /// FillTimestampUs - SubmittedTimestampUs。
    /// </summary>
    public long MarketFillDelayUs => FillTimestampUs - SubmittedTimestampUs;

    /// <summary>
    /// 從預定成交到實際成交的總延遲。
    ///
    /// FillTimestampUs - DueTimestampUs。
    /// </summary>
    public long TotalDelayUs => FillTimestampUs - DueTimestampUs;

    /// <summary>
    /// 策略決策價格 e4。
    ///
    /// 例如策略看到 latest price 後決定下單。
    /// </summary>
    public int DecisionPriceE4 { get; init; }

    public int? RiskReferencePriceE4 { get; init; }

    /// <summary>
    /// 未套用滑價前的成交價格 e4。
    ///
    /// 第一版未接滑價時可等於 FillPriceE4。
    /// </summary>
    public int RawFillPriceE4 { get; init; }

    /// <summary>
    /// 最終成交價格 e4。
    ///
    /// 未來接滑價後，這是套用滑價後的價格。
    /// </summary>
    public int FillPriceE4 { get; init; }

    public int RawSelectedBidPriceE4 { get; init; }

    public int RawSelectedAskPriceE4 { get; init; }

    public int RawSelectedLatestPriceE4 { get; init; }

    public string QuoteConsistencyStatus { get; init; } = "Normal";

    public bool ConservativeFillApplied { get; init; }

    public string ConservativeFillReason { get; init; } = "None";

    /// <summary>
    /// 滑價 tick 數。
    ///
    /// 第一版未接滑價時為 0。
    /// </summary>
    public int SlippageTicks { get; init; }

    /// <summary>
    /// 滑價造成的價格差 e4。
    ///
    /// 第一版未接滑價時為 0。
    /// </summary>
    public int SlippagePriceE4 { get; init; }

    /// <summary>
    /// 成交張數。
    /// </summary>
    public int Lots { get; init; }

    /// <summary>
    /// 每張股數。
    ///
    /// 台股一般股票為 1000。
    /// </summary>
    public int LotSizeShares { get; init; } = 1000;

    /// <summary>
    /// 成交股數。
    /// </summary>
    public long Shares => (long)Lots * LotSizeShares;

    /// <summary>
    /// 成交前持倉張數。
    ///
    /// 通用規則：
    /// - 多單為正數
    /// - 空單為負數
    /// - 空手為 0
    /// </summary>
    public int PositionLotsBefore { get; init; }

    /// <summary>
    /// 成交後持倉張數。
    ///
    /// 通用規則：
    /// - 多單為正數
    /// - 空單為負數
    /// - 空手為 0
    /// </summary>
    public int PositionLotsAfter { get; init; }

    /// <summary>
    /// 成交前平均進場價 e4。
    /// </summary>
    public int AverageEntryPriceE4Before { get; init; }

    /// <summary>
    /// 成交後平均進場價 e4。
    /// </summary>
    public int AverageEntryPriceE4After { get; init; }

    /// <summary>
    /// 成交金額，單位 TWD。
    ///
    /// GrossAmountTwd = FillPrice × Shares。
    /// 不含手續費與稅。
    /// </summary>
    public long GrossAmountTwd { get; init; }

    /// <summary>
    /// 這筆成交產生的已實現毛損益，單位 TWD。
    ///
    /// 開倉或加倉通常為 0。
    /// 減倉、平倉、強制平倉時才會有值。
    /// </summary>
    public long GrossPnlTwd { get; init; }

    /// <summary>
    /// 這筆成交的券商手續費，單位 TWD。
    /// </summary>
    public long CommissionTwd { get; init; }

    /// <summary>
    /// 這筆成交的證交稅，單位 TWD。
    /// </summary>
    public long TaxTwd { get; init; }

    /// <summary>
    /// 這筆成交產生的已實現淨損益，單位 TWD。
    ///
    /// NetPnlTwd = GrossPnlTwd - CommissionTwd - TaxTwd。
    /// </summary>
    public long NetPnlTwd { get; init; }

    /// <summary>
    /// 現金流，單位 TWD。
    ///
    /// 買進通常為負數。
    /// 賣出通常為正數。
    /// 手續費與稅應反映在現金流中。
    ///
    /// 第一版未接 accounting 時可以先為 0。
    /// </summary>
    public long NetCashFlowTwd { get; init; }

    /// <summary>
    /// 備註。
    /// </summary>
    public string Note { get; init; } = "";
}
