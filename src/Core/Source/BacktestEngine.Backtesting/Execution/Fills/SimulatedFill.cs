using BacktestEngine.Backtesting.Execution.Records;
using BacktestEngine.Contracts.Orders;
using BacktestEngine.Contracts.Signals;

namespace BacktestEngine.Backtesting.Execution.Fills;

/// <summary>
/// ExecutionSimulator 產生的單筆模擬成交。
///
/// 這是 accounting 的輸入。
/// 它還不是最終交易紀錄；
/// 最終 SimulatedTradeRecord 由 SimulatedAccountingEngine 根據 fill + position 產生。
/// </summary>
public sealed class SimulatedFill
{
    /// <summary>
    /// 成交序號。
    /// </summary>
    public long Sequence { get; init; }

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
    /// 成交方向。
    /// </summary>
    public SimulatedTradeSide Side { get; init; }

    /// <summary>
    /// 策略意圖大類。
    /// </summary>
    public SimulatedTradeIntentKind IntentKind { get; init; }

    /// <summary>
    /// 策略 trigger 數字 ID。
    /// </summary>
    public int TriggerId { get; init; }

    /// <summary>
    /// 策略 trigger code。
    /// </summary>
    public string TriggerCode { get; init; } = "";

    /// <summary>
    /// trigger 分組。
    /// </summary>
    public string TriggerGroup { get; init; } = "";

    /// <summary>
    /// 人類可讀 trigger 名稱。
    /// </summary>
    public string TriggerName { get; init; } = "";

    /// <summary>
    /// trigger 詳細說明。
    /// </summary>
    public string TriggerDetail { get; init; } = "";

    public IReadOnlyList<TriggerEvidenceItem> TriggerEvidence { get; init; } =
        Array.Empty<TriggerEvidenceItem>();

    public Guid EntryCycleId { get; init; }

    public Guid CorrelationId { get; init; }

    /// <summary>
    /// 觸發訊號時間。
    /// </summary>
    public long TriggerTimestampUs { get; init; }

    /// <summary>
    /// 觸發訊號價格 e4。
    /// </summary>
    public int TriggerPriceE4 { get; init; }

    /// <summary>
    /// 預定成交時間。
    /// </summary>
    public long DueTimestampUs { get; init; }

    /// <summary>
    /// 模擬送單時間。
    /// </summary>
    public long SubmittedTimestampUs { get; init; }

    public long OrderArrivalTimestampUs { get; init; }

    public long MarketEventTimestampUsedUs { get; init; }

    public long FillTimeDeltaUs => MarketEventTimestampUsedUs - OrderArrivalTimestampUs;

    public ExecutionFillTimingMode FillTimingMode { get; init; } =
        ExecutionFillTimingMode.ClockDrivenLastKnown;

    /// <summary>
    /// 實際模擬成交時間。
    /// </summary>
    public long FillTimestampUs { get; init; }

    /// <summary>
    /// 策略決策價格 e4。
    /// </summary>
    public int DecisionPriceE4 { get; init; }

    public int? RiskReferencePriceE4 { get; init; }

    /// <summary>
    /// 未套用滑價前成交價格 e4。
    /// </summary>
    public int RawFillPriceE4 { get; init; }

    public ExecutionResolvedFillPriceSource FillPriceSource { get; init; } =
        ExecutionResolvedFillPriceSource.Unknown;

    public int MarketBidPriceE4 { get; init; }

    public int MarketAskPriceE4 { get; init; }

    public int MarketLatestPriceE4 { get; init; }

    public int RawSelectedBidPriceE4 { get; init; }

    public int RawSelectedAskPriceE4 { get; init; }

    public int RawSelectedLatestPriceE4 { get; init; }

    public string QuoteConsistencyStatus { get; init; } = "Normal";

    public bool ConservativeFillApplied { get; init; }

    public string ConservativeFillReason { get; init; } = "None";

    /// <summary>
    /// 最終成交價格 e4。
    /// </summary>
    public int FillPriceE4 { get; init; }

    /// <summary>
    /// 滑價 tick 數。
    /// </summary>
    public int SlippageTicks { get; init; }

    /// <summary>
    /// 滑價價格差 e4。
    /// </summary>
    public int SlippagePriceE4 { get; init; }

    /// <summary>
    /// 成交張數。
    /// </summary>
    public int Lots { get; init; }

    /// <summary>
    /// 每張股數。
    /// </summary>
    public int LotSizeShares { get; init; } = 1000;

    /// <summary>
    /// 成交股數。
    /// </summary>
    public long Shares => (long)Lots * LotSizeShares;

    /// <summary>
    /// 備註。
    /// </summary>
    public string Note { get; init; } = "";

    /// <summary>
    /// 基本資料驗證。
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(StrategyId))
            throw new InvalidOperationException("StrategyId 不可為空。");

        if (string.IsNullOrWhiteSpace(Symbol))
            throw new InvalidOperationException("Symbol 不可為空。");

        if (TradingDate == default)
            throw new InvalidOperationException("TradingDate 不可為 default。");

        if (Side is not SimulatedTradeSide.Buy
            and not SimulatedTradeSide.Sell)
        {
            throw new InvalidOperationException("Side 必須是 Buy 或 Sell。");
        }

        if (FillPriceE4 <= 0)
            throw new InvalidOperationException("FillPriceE4 必須大於 0。");

        if (Lots <= 0)
            throw new InvalidOperationException("Lots 必須大於 0。");

        if (LotSizeShares <= 0)
            throw new InvalidOperationException("LotSizeShares 必須大於 0。");
    }
}
