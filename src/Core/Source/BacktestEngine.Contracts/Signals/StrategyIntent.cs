namespace BacktestEngine.Contracts.Signals;

/// <summary>
/// 策略產生的交易意圖。
///
/// StrategyIntent 是 strategy 與 execution 的正式交界。
///
/// 策略只負責說：
/// - 哪個策略
/// - 哪個商品
/// - 哪個交易日
/// - 買或賣
/// - 進場或出場
/// - 幾張
/// - 為什麼
/// - 預定何時執行
///
/// 策略不負責：
/// - 實際成交價
/// - 滑價
/// - 延遲
/// - 手續費
/// - 證交稅
/// - position effect
/// - realized pnl
///
/// 這些都由 ExecutionSimulator / SimulatedAccountingEngine 處理。
/// </summary>
public sealed class StrategyIntent
{
    /// <summary>
    /// 策略意圖序號。
    ///
    /// 第一版可以由策略 runtime 遞增；
    /// 若暫時沒有序號，可先為 0。
    /// </summary>
    public long Sequence { get; init; }

    /// <summary>
    /// 策略 ID。
    ///
    /// 範例：
    /// - intraday_strength_fade
    /// - intraday_trend
    /// </summary>
    public required string StrategyId { get; init; }

    /// <summary>
    /// 策略執行實例 ID。
    ///
    /// 同一策略同一天跑多組參數時，可用它區分。
    /// 第一版可為空字串。
    /// </summary>
    public string StrategyRunId { get; init; } = "";

    /// <summary>
    /// 商品代碼。
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// 交易日。
    /// </summary>
    public DateOnly TradingDate { get; init; }

    /// <summary>
    /// 策略意圖大類。
    /// </summary>
    public StrategyIntentKind Kind { get; init; }

    /// <summary>
    /// 買賣方向。
    /// </summary>
    public StrategyIntentSide Side { get; init; }

    /// <summary>
    /// 目標成交張數。
    /// </summary>
    public int Lots { get; init; }

    /// <summary>
    /// trigger identity。
    /// </summary>
    public required StrategyIntentCode Code { get; init; }

    /// <summary>
    /// Strategy V2 entry cycle id。
    /// Entry 會建立新 cycle；Exit 使用目前 position 的 cycle。
    /// 舊流程可維持 Guid.Empty。
    /// </summary>
    public Guid EntryCycleId { get; init; }

    /// <summary>
    /// 同一次策略決策產生的 correlation id。
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Strategy V2 數量語意。
    /// </summary>
    public StrategyQuantityIntent QuantityIntent { get; init; }

    /// <summary>
    /// PriorityPolicy 選出的 primary trigger。
    /// </summary>
    public StrategyIntentCode? PrimaryTriggerCode { get; init; }

    public IReadOnlyList<TriggerEvidenceItem> PrimaryTriggerEvidence { get; init; } =
        Array.Empty<TriggerEvidenceItem>();

    /// <summary>
    /// PriorityPolicy 保留下來的 secondary candidates。
    /// </summary>
    public IReadOnlyList<MatchedTriggerCandidate> MatchedTriggerCandidates { get; init; } =
        Array.Empty<MatchedTriggerCandidate>();

    /// <summary>
    /// 策略決策時採用的價格 e4。
    /// </summary>
    public int DecisionPriceE4 { get; init; }

    public int? RiskReferencePriceE4 { get; init; }

    /// <summary>
    /// 觸發策略訊號的時間。
    /// </summary>
    public long TriggerTimestampUs { get; init; }

    /// <summary>
    /// 觸發策略訊號時的價格 e4。
    /// </summary>
    public int TriggerPriceE4 { get; init; }

    /// <summary>
    /// 預定執行時間。
    ///
    /// ExecutionSimulator 會根據這個時間、fill timing mode、
    /// latency 與 market snapshot 決定最終 fill。
    /// </summary>
    public long DueTimestampUs { get; init; }

    /// <summary>
    /// 備註。
    /// </summary>
    public string Note { get; init; } = "";

    /// <summary>
    /// 是否為進場意圖。
    /// </summary>
    public bool IsEntry => Kind == StrategyIntentKind.Entry;

    /// <summary>
    /// 是否為出場意圖。
    /// </summary>
    public bool IsExit => Kind == StrategyIntentKind.Exit;

    /// <summary>
    /// 是否為取消 pending entry 意圖。
    /// </summary>
    public bool IsCancelPendingEntry => Kind == StrategyIntentKind.CancelPendingEntry;

    /// <summary>
    /// 是否為買進意圖。
    /// </summary>
    public bool IsBuy => Side == StrategyIntentSide.Buy;

    /// <summary>
    /// 是否為賣出意圖。
    /// </summary>
    public bool IsSell => Side == StrategyIntentSide.Sell;

    /// <summary>
    /// 驗證策略意圖。
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(StrategyId))
            throw new InvalidOperationException("StrategyId 不可為空。");

        if (string.IsNullOrWhiteSpace(Symbol))
            throw new InvalidOperationException("Symbol 不可為空。");

        if (TradingDate == default)
            throw new InvalidOperationException("TradingDate 不可為 default。");

        if (Kind is not StrategyIntentKind.Entry
            and not StrategyIntentKind.Exit
            and not StrategyIntentKind.CancelPendingEntry)
        {
            throw new InvalidOperationException("Kind 必須是 Entry、Exit 或 CancelPendingEntry。");
        }

        if (Kind == StrategyIntentKind.CancelPendingEntry)
        {
            if (QuantityIntent != StrategyQuantityIntent.CancelPendingEntry)
                throw new InvalidOperationException("CancelPendingEntry 必須搭配 QuantityIntent.CancelPendingEntry。");

            Code.Validate();
            return;
        }

        if (Side is not StrategyIntentSide.Buy
            and not StrategyIntentSide.Sell)
        {
            throw new InvalidOperationException("Side 必須是 Buy 或 Sell。");
        }

        if (Lots <= 0
            && QuantityIntent is not StrategyQuantityIntent.OpenBySizingProfile
                and not StrategyQuantityIntent.CloseAll)
        {
            throw new InvalidOperationException("Lots 必須大於 0。");
        }

        if (TriggerTimestampUs <= 0)
            throw new InvalidOperationException("TriggerTimestampUs 必須大於 0。");

        if (TriggerPriceE4 <= 0)
            throw new InvalidOperationException("TriggerPriceE4 必須大於 0。");

        if (DueTimestampUs <= 0)
            throw new InvalidOperationException("DueTimestampUs 必須大於 0。");

        Code.Validate();
    }
}
