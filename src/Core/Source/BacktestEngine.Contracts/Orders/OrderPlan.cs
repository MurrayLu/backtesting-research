using BacktestEngine.Contracts.Signals;

namespace BacktestEngine.Contracts.Orders;

/// <summary>
/// 策略提出的下單計畫。
///
/// OrderPlan 不是成交，也不是 pending order。
/// 它是 strategy runtime 用來描述：
/// - 我要買 / 賣
/// - 我要進場 / 出場
/// - 總共幾張
/// - 從什麼時間開始執行
/// - 是否要依 execution schedule 分批
///
/// 後續由 OrderPlanIntentFactory 轉成一筆或多筆 StrategyIntent。
/// </summary>
public sealed class OrderPlan
{
    /// <summary>
    /// 策略 ID。
    /// </summary>
    public required string StrategyId { get; init; }

    /// <summary>
    /// 策略執行實例 ID。
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
    /// 進場 / 出場意圖。
    /// </summary>
    public StrategyIntentKind Kind { get; init; }

    /// <summary>
    /// 買 / 賣方向。
    /// </summary>
    public StrategyIntentSide Side { get; init; }

    /// <summary>
    /// 總張數。
    /// </summary>
    public int TotalLots { get; init; }

    /// <summary>
    /// trigger identity。
    /// </summary>
    public required StrategyIntentCode Code { get; init; }

    public Guid EntryCycleId { get; init; }

    public Guid CorrelationId { get; init; }

    public StrategyQuantityIntent QuantityIntent { get; init; }

    public StrategyIntentCode? PrimaryTriggerCode { get; init; }

    public IReadOnlyList<TriggerEvidenceItem> PrimaryTriggerEvidence { get; init; } =
        Array.Empty<TriggerEvidenceItem>();

    public IReadOnlyList<MatchedTriggerCandidate> MatchedTriggerCandidates { get; init; } =
        Array.Empty<MatchedTriggerCandidate>();

    public int? RiskReferencePriceE4 { get; init; }

    /// <summary>
    /// 策略訊號成立時間。
    /// </summary>
    public long TriggerTimestampUs { get; init; }

    /// <summary>
    /// 策略訊號成立價格 e4。
    /// </summary>
    public int TriggerPriceE4 { get; init; }

    /// <summary>
    /// 第一筆 intent 的預定執行時間。
    /// </summary>
    public long FirstDueTimestampUs { get; init; }

    /// <summary>
    /// 分批模式。
    /// </summary>
    public OrderPlanBatchMode BatchMode { get; init; } =
        OrderPlanBatchMode.UseScheduleDefault;

    /// <summary>
    /// 備註。
    /// </summary>
    public string Note { get; init; } = "";

    /// <summary>
    /// 驗證 OrderPlan。
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
            throw new InvalidOperationException("Kind 必須是 Entry 或 Exit。");
        }

        if (Kind == StrategyIntentKind.CancelPendingEntry)
        {
            ArgumentNullException.ThrowIfNull(Code);
            Code.Validate();
            return;
        }

        if (Side is not StrategyIntentSide.Buy
            and not StrategyIntentSide.Sell)
        {
            throw new InvalidOperationException("Side 必須是 Buy 或 Sell。");
        }

        if (TotalLots <= 0)
            throw new InvalidOperationException("TotalLots 必須大於 0。");

        ArgumentNullException.ThrowIfNull(Code);
        Code.Validate();

        if (TriggerTimestampUs <= 0)
            throw new InvalidOperationException("TriggerTimestampUs 必須大於 0。");

        if (TriggerPriceE4 <= 0)
            throw new InvalidOperationException("TriggerPriceE4 必須大於 0。");

        if (FirstDueTimestampUs <= 0)
            throw new InvalidOperationException("FirstDueTimestampUs 必須大於 0。");

        if (FirstDueTimestampUs < TriggerTimestampUs)
            throw new InvalidOperationException("FirstDueTimestampUs 不可早於 TriggerTimestampUs。");

        if (BatchMode is not OrderPlanBatchMode.UseScheduleDefault
            and not OrderPlanBatchMode.ForceSingle
            and not OrderPlanBatchMode.ForceBatch)
        {
            throw new InvalidOperationException("BatchMode 必須是有效值。");
        }
    }
}
