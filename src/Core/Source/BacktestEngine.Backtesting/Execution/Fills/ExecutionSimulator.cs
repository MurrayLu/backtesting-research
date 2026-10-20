using BacktestEngine.Backtesting.Execution.Records;
using BacktestEngine.Contracts.Common;
using BacktestEngine.Contracts.Orders;
using BacktestEngine.Contracts.Signals;

namespace BacktestEngine.Backtesting.Execution.Fills;

/// <summary>
/// 通用 execution simulator。
///
/// 職責：
/// - 接收 StrategyIntent。
/// - 根據 due time 與 order latency 判斷是否可送單 / 成交。
/// - 根據 MarketSnapshot 決定 raw fill price。
/// - 套用滑價。
/// - 產生 SimulatedFill。
///
/// 不負責：
/// - position effect
/// - average entry price
/// - realized pnl
/// - fee / tax
/// - net pnl
///
/// 以上交給 SimulatedAccountingEngine。
/// </summary>
public sealed class ExecutionSimulator
{
    private readonly ExecutionModelSpec _spec;

    /// <summary>
    /// 使用預設台股當沖 execution model。
    /// </summary>
    public ExecutionSimulator()
        : this(ExecutionModelSpec.CreateDefaultTaiwanStockIntraday())
    {
    }

    /// <summary>
    /// 使用指定 execution model。
    /// </summary>
    public ExecutionSimulator(ExecutionModelSpec spec)
    {
        _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        _spec.Validate();
    }

    /// <summary>
    /// 嘗試把 strategy intent 轉成 simulated fill。
    ///
    /// NextTickOnly 成交規則：
    /// - snapshot.TimestampUs 必須大於 SubmittedTimestampUs 才能成交。
    /// - snapshot.TimestampUs 等於 SubmittedTimestampUs 時不成交。
    /// - 這是為了避免同 timestamp 無法判斷訂單與市場事件先後順序。
    /// </summary>
    public ExecutionSimulationResult TryExecute(
        StrategyIntent intent,
        in MarketSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(intent);
        intent.Validate();

        ValidateSnapshot(snapshot);

        if (snapshot.TimestampUs < intent.DueTimestampUs)
            return ExecutionSimulationResult.Rejected(ExecutionRejectReason.IntentNotDue);

        var submittedTimestampUs =
            intent.DueTimestampUs + _spec.OrderLatency.FixedLatencyUs;

        if (snapshot.TimestampUs < submittedTimestampUs)
            return ExecutionSimulationResult.Rejected(ExecutionRejectReason.OrderLatencyNotReached);

        if (snapshot.TimestampUs == submittedTimestampUs)
            return ExecutionSimulationResult.Rejected(ExecutionRejectReason.WaitingNextSnapshotAfterOrderArrival);

        return Fill(
            intent,
            snapshot,
            submittedTimestampUs,
            ExecutionFillTimingMode.ClockDrivenLastKnown);
    }

    public ExecutionSimulationResult TryExecuteAtMarketEvent(
        StrategyIntent intent,
        in MarketSnapshot marketEvent,
        ExecutionFillTimingMode fillTimingMode)
    {
        ArgumentNullException.ThrowIfNull(intent);
        intent.Validate();

        ValidateSnapshot(marketEvent);

        var submittedTimestampUs = GetSubmittedTimestampUs(intent);

        return Fill(
            intent,
            marketEvent,
            submittedTimestampUs,
            fillTimingMode);
    }

    public ExecutionSimulationResult TryExecute(
        OrderPlan plan,
        in MarketSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.Validate();

        ValidateSnapshot(snapshot);

        if (plan.Kind == StrategyIntentKind.CancelPendingEntry)
            return ExecutionSimulationResult.Rejected(ExecutionRejectReason.CancelOrderDoesNotFill);

        if (snapshot.TimestampUs < plan.FirstDueTimestampUs)
            return ExecutionSimulationResult.Rejected(ExecutionRejectReason.IntentNotDue);

        var submittedTimestampUs = GetSubmittedTimestampUs(plan);

        if (snapshot.TimestampUs < submittedTimestampUs)
            return ExecutionSimulationResult.Rejected(ExecutionRejectReason.OrderLatencyNotReached);

        return Fill(
            plan,
            snapshot,
            submittedTimestampUs,
            ExecutionFillTimingMode.ClockDrivenLastKnown);
    }

    public ExecutionSimulationResult TryExecuteAtMarketEvent(
        OrderPlan plan,
        in MarketSnapshot marketEvent,
        ExecutionFillTimingMode fillTimingMode)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.Validate();

        ValidateSnapshot(marketEvent);

        if (plan.Kind == StrategyIntentKind.CancelPendingEntry)
            return ExecutionSimulationResult.Rejected(ExecutionRejectReason.CancelOrderDoesNotFill);

        return Fill(
            plan,
            marketEvent,
            GetSubmittedTimestampUs(plan),
            fillTimingMode);
    }

    public long GetSubmittedTimestampUs(
        StrategyIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        intent.Validate();

        return intent.DueTimestampUs + _spec.OrderLatency.FixedLatencyUs;
    }

    public long GetSubmittedTimestampUs(
        OrderPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.Validate();

        return plan.FirstDueTimestampUs + _spec.OrderLatency.FixedLatencyUs;
    }

    private ExecutionSimulationResult Fill(
        StrategyIntent intent,
        in MarketSnapshot snapshot,
        long submittedTimestampUs,
        ExecutionFillTimingMode fillTimingMode)
    {

        var selectedFillPrice = ResolveRawFillPrice(
            intent.Side,
            snapshot);
        var guardedFillPrice = ApplyQuoteConsistencyGuard(
            intent,
            snapshot,
            selectedFillPrice);
        var rawFillPriceE4 = guardedFillPrice.PriceE4;

        if (rawFillPriceE4 <= 0)
            return ExecutionSimulationResult.Rejected(ExecutionRejectReason.NoFillPrice);

        var fillPriceE4 = ApplySlippage(
            intent.Side,
            rawFillPriceE4);

        var slippagePriceE4 = fillPriceE4 - rawFillPriceE4;

        if (fillPriceE4 <= 0)
            return ExecutionSimulationResult.Rejected(ExecutionRejectReason.NoFillPrice);

        var fill = new SimulatedFill
        {
            Sequence = intent.Sequence,
            StrategyId = intent.StrategyId,
            StrategyRunId = intent.StrategyRunId,
            Symbol = intent.Symbol,
            TradingDate = intent.TradingDate,

            Side = MapSide(intent.Side),
            IntentKind = MapIntentKind(intent.Kind),

            TriggerId = intent.Code.TriggerId,
            TriggerCode = intent.Code.TriggerCode,
            TriggerGroup = intent.Code.TriggerGroup,
            TriggerName = intent.Code.TriggerName,
            TriggerDetail = intent.Code.TriggerDetail,
            TriggerEvidence = intent.PrimaryTriggerEvidence,
            EntryCycleId = intent.EntryCycleId,
            CorrelationId = intent.CorrelationId,

            TriggerTimestampUs = intent.TriggerTimestampUs,
            TriggerPriceE4 = intent.TriggerPriceE4,

            DueTimestampUs = intent.DueTimestampUs,
            SubmittedTimestampUs = submittedTimestampUs,
            OrderArrivalTimestampUs = submittedTimestampUs,
            MarketEventTimestampUsedUs = snapshot.TimestampUs,
            FillTimingMode = fillTimingMode,
            FillTimestampUs = submittedTimestampUs,

            DecisionPriceE4 = intent.TriggerPriceE4,
            RawFillPriceE4 = rawFillPriceE4,
            FillPriceSource = guardedFillPrice.Source,
            MarketBidPriceE4 = snapshot.BidPriceE4,
            MarketAskPriceE4 = snapshot.AskPriceE4,
            MarketLatestPriceE4 = snapshot.LatestPriceE4,
            RawSelectedBidPriceE4 = snapshot.BidPriceE4,
            RawSelectedAskPriceE4 = snapshot.AskPriceE4,
            RawSelectedLatestPriceE4 = snapshot.LatestPriceE4,
            RiskReferencePriceE4 = intent.RiskReferencePriceE4,
            QuoteConsistencyStatus = guardedFillPrice.QuoteConsistencyStatus,
            ConservativeFillApplied = guardedFillPrice.ConservativeFillApplied,
            ConservativeFillReason = guardedFillPrice.ConservativeFillReason,
            FillPriceE4 = fillPriceE4,

            SlippageTicks = _spec.Slippage.FixedSlippageTicks,
            SlippagePriceE4 = slippagePriceE4,

            Lots = intent.Lots,
            LotSizeShares = 1000,

            Note = intent.Note
        };

        fill.Validate();

        return ExecutionSimulationResult.Filled(fill);
    }

    private ExecutionSimulationResult Fill(
        OrderPlan plan,
        in MarketSnapshot snapshot,
        long submittedTimestampUs,
        ExecutionFillTimingMode fillTimingMode)
    {

        var selectedFillPrice = ResolveStrictBidAskFillPrice(
            plan.Side,
            snapshot);
        if (selectedFillPrice.RejectReason != ExecutionRejectReason.None)
            return ExecutionSimulationResult.Rejected(selectedFillPrice.RejectReason);

        var rawFillPriceE4 = selectedFillPrice.PriceE4;

        if (rawFillPriceE4 <= 0)
            return ExecutionSimulationResult.Rejected(ExecutionRejectReason.NoFillPrice);

        var fillPriceE4 = ApplySlippage(
            plan.Side,
            rawFillPriceE4);

        var slippagePriceE4 = fillPriceE4 - rawFillPriceE4;

        if (fillPriceE4 <= 0)
            return ExecutionSimulationResult.Rejected(ExecutionRejectReason.NoFillPrice);

        var fill = new SimulatedFill
        {
            Sequence = 0,
            StrategyId = plan.StrategyId,
            StrategyRunId = plan.StrategyRunId,
            Symbol = plan.Symbol,
            TradingDate = plan.TradingDate,

            Side = MapSide(plan.Side),
            IntentKind = MapIntentKind(plan.Kind),

            TriggerId = plan.Code.TriggerId,
            TriggerCode = plan.Code.TriggerCode,
            TriggerGroup = plan.Code.TriggerGroup,
            TriggerName = plan.Code.TriggerName,
            TriggerDetail = plan.Code.TriggerDetail,
            TriggerEvidence = plan.PrimaryTriggerEvidence,
            EntryCycleId = plan.EntryCycleId,
            CorrelationId = plan.CorrelationId,

            TriggerTimestampUs = plan.TriggerTimestampUs,
            TriggerPriceE4 = plan.TriggerPriceE4,

            DueTimestampUs = plan.FirstDueTimestampUs,
            SubmittedTimestampUs = submittedTimestampUs,
            OrderArrivalTimestampUs = submittedTimestampUs,
            MarketEventTimestampUsedUs = snapshot.TimestampUs,
            FillTimingMode = fillTimingMode,
            FillTimestampUs = submittedTimestampUs,

            DecisionPriceE4 = plan.TriggerPriceE4,
            RawFillPriceE4 = rawFillPriceE4,
            FillPriceSource = selectedFillPrice.Source,
            MarketBidPriceE4 = snapshot.BidPriceE4,
            MarketAskPriceE4 = snapshot.AskPriceE4,
            MarketLatestPriceE4 = snapshot.LatestPriceE4,
            RawSelectedBidPriceE4 = snapshot.BidPriceE4,
            RawSelectedAskPriceE4 = snapshot.AskPriceE4,
            RawSelectedLatestPriceE4 = snapshot.LatestPriceE4,
            RiskReferencePriceE4 = plan.RiskReferencePriceE4,
            FillPriceE4 = fillPriceE4,

            SlippageTicks = _spec.Slippage.FixedSlippageTicks,
            SlippagePriceE4 = slippagePriceE4,

            Lots = plan.TotalLots,
            LotSizeShares = 1000,

            Note = plan.Note
        };

        fill.Validate();

        return ExecutionSimulationResult.Filled(fill);
    }

    private FillPriceResolution ApplyQuoteConsistencyGuard(
        StrategyIntent intent,
        in MarketSnapshot snapshot,
        FillPriceResolution selectedFillPrice)
    {
        if (selectedFillPrice.PriceE4 <= 0)
        {
            return selectedFillPrice;
        }

        if (!_spec.ConservativeBidAskLatestGuard)
            return selectedFillPrice;

        var result = ApplyBidAskLatestGuard(
            intent,
            snapshot,
            selectedFillPrice);

        return ApplyRiskReferenceGuard(
            intent,
            result);
    }

    private static FillPriceResolution ApplyBidAskLatestGuard(
        StrategyIntent intent,
        in MarketSnapshot snapshot,
        FillPriceResolution selectedFillPrice)
    {
        if (snapshot.LatestPriceE4 <= 0)
            return selectedFillPrice;

        if (intent.Side == StrategyIntentSide.Sell
            && selectedFillPrice.Source == ExecutionResolvedFillPriceSource.Bid
            && selectedFillPrice.PriceE4 > snapshot.LatestPriceE4)
        {
            return selectedFillPrice with
            {
                PriceE4 = snapshot.LatestPriceE4,
                Source = ExecutionResolvedFillPriceSource.BidConservativeLatest,
                QuoteConsistencyStatus = "QuoteBetterThanLatestAdjusted",
                ConservativeFillApplied = true,
                ConservativeFillReason = "SellBidAboveLatest"
            };
        }

        if (intent.Side == StrategyIntentSide.Buy
            && selectedFillPrice.Source == ExecutionResolvedFillPriceSource.Ask
            && selectedFillPrice.PriceE4 < snapshot.LatestPriceE4)
        {
            return selectedFillPrice with
            {
                PriceE4 = snapshot.LatestPriceE4,
                Source = ExecutionResolvedFillPriceSource.AskConservativeLatest,
                QuoteConsistencyStatus = "QuoteBetterThanLatestAdjusted",
                ConservativeFillApplied = true,
                ConservativeFillReason = "BuyAskBelowLatest"
            };
        }

        return selectedFillPrice;
    }

    private static FillPriceResolution ApplyRiskReferenceGuard(
        StrategyIntent intent,
        FillPriceResolution selectedFillPrice)
    {
        if (intent.Side != StrategyIntentSide.Buy
            || intent.RiskReferencePriceE4 is not int riskReferencePriceE4
            || riskReferencePriceE4 <= 0
            || selectedFillPrice.PriceE4 >= riskReferencePriceE4)
        {
            return selectedFillPrice;
        }

        return selectedFillPrice with
        {
            PriceE4 = riskReferencePriceE4,
            Source = ExecutionResolvedFillPriceSource.HardStopConservativeLatest,
            QuoteConsistencyStatus = "HardStopRiskReferenceAdjusted",
            ConservativeFillApplied = true,
            ConservativeFillReason = "HardStopRiskReferenceAboveAsk"
        };
    }

    private static void ValidateSnapshot(in MarketSnapshot snapshot)
    {
        if (snapshot.TimestampUs <= 0)
            throw new InvalidOperationException("Snapshot.TimestampUs 必須大於 0。");

        if (snapshot.LatestPriceE4 <= 0
            && snapshot.BidPriceE4 <= 0
            && snapshot.AskPriceE4 <= 0)
        {
            throw new InvalidOperationException("Snapshot 至少需要 latest / bid / ask 其中一種價格。");
        }
    }

    private FillPriceResolution ResolveRawFillPrice(
        StrategyIntentSide side,
        in MarketSnapshot snapshot)
    {
        return _spec.FillPriceSource switch
        {
            ExecutionFillPriceSource.LatestPrice =>
                new FillPriceResolution(
                    snapshot.LatestPriceE4,
                    ExecutionResolvedFillPriceSource.Latest),

            ExecutionFillPriceSource.BidAskWithLatestFallback =>
                ResolveBidAskWithLatestFallback(side, snapshot),

            _ => new FillPriceResolution(0, ExecutionResolvedFillPriceSource.Unknown)
        };
    }

    private static FillPriceResolution ResolveBidAskWithLatestFallback(
        StrategyIntentSide side,
        in MarketSnapshot snapshot)
    {
        return side switch
        {
            StrategyIntentSide.Buy =>
                snapshot.AskPriceE4 > 0
                    ? new FillPriceResolution(
                        snapshot.AskPriceE4,
                        ExecutionResolvedFillPriceSource.Ask)
                    : new FillPriceResolution(
                        snapshot.LatestPriceE4,
                        ExecutionResolvedFillPriceSource.LatestFallback),

            StrategyIntentSide.Sell =>
                snapshot.BidPriceE4 > 0
                    ? new FillPriceResolution(
                        snapshot.BidPriceE4,
                        ExecutionResolvedFillPriceSource.Bid)
                    : new FillPriceResolution(
                        snapshot.LatestPriceE4,
                        ExecutionResolvedFillPriceSource.LatestFallback),

            _ => new FillPriceResolution(0, ExecutionResolvedFillPriceSource.Unknown)
        };
    }

    private static StrictFillPriceResolution ResolveStrictBidAskFillPrice(
        StrategyIntentSide side,
        in MarketSnapshot snapshot)
    {
        return side switch
        {
            StrategyIntentSide.Buy =>
                snapshot.AskPriceE4 > 0
                    ? new StrictFillPriceResolution(
                        snapshot.AskPriceE4,
                        ExecutionResolvedFillPriceSource.Ask,
                        ExecutionRejectReason.None)
                    : new StrictFillPriceResolution(
                        0,
                        ExecutionResolvedFillPriceSource.Unknown,
                        ExecutionRejectReason.MissingAskPrice),

            StrategyIntentSide.Sell =>
                snapshot.BidPriceE4 > 0
                    ? new StrictFillPriceResolution(
                        snapshot.BidPriceE4,
                        ExecutionResolvedFillPriceSource.Bid,
                        ExecutionRejectReason.None)
                    : new StrictFillPriceResolution(
                        0,
                        ExecutionResolvedFillPriceSource.Unknown,
                        ExecutionRejectReason.MissingBidPrice),

            _ => new StrictFillPriceResolution(
                0,
                ExecutionResolvedFillPriceSource.Unknown,
                ExecutionRejectReason.NoFillPrice)
        };
    }

    private int ApplySlippage(
        StrategyIntentSide side,
        int rawFillPriceE4)
    {
        return side switch
        {
            StrategyIntentSide.Buy =>
                _spec.Slippage.ApplyBuySlippage(rawFillPriceE4),

            StrategyIntentSide.Sell =>
                _spec.Slippage.ApplySellSlippage(rawFillPriceE4),

            _ => rawFillPriceE4
        };
    }

    private static SimulatedTradeSide MapSide(StrategyIntentSide side)
    {
        return side switch
        {
            StrategyIntentSide.Buy => SimulatedTradeSide.Buy,
            StrategyIntentSide.Sell => SimulatedTradeSide.Sell,
            _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Unknown strategy intent side.")
        };
    }

    private static SimulatedTradeIntentKind MapIntentKind(StrategyIntentKind kind)
    {
        return kind switch
        {
            StrategyIntentKind.Entry => SimulatedTradeIntentKind.Entry,
            StrategyIntentKind.Exit => SimulatedTradeIntentKind.Exit,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown strategy intent kind.")
        };
    }

    private readonly record struct FillPriceResolution(
        int PriceE4,
        ExecutionResolvedFillPriceSource Source,
        string QuoteConsistencyStatus = "Normal",
        bool ConservativeFillApplied = false,
        string ConservativeFillReason = "None");

    private readonly record struct StrictFillPriceResolution(
        int PriceE4,
        ExecutionResolvedFillPriceSource Source,
        ExecutionRejectReason RejectReason);
}
