using BacktestEngine.Backtesting.Execution.Accounting;
using BacktestEngine.Backtesting.Execution.Fills;
using BacktestEngine.Backtesting.Execution.Records;
using BacktestEngine.Contracts.Common;
using BacktestEngine.Contracts.Orders;
using BacktestEngine.Contracts.Signals;

namespace BacktestEngine.Backtesting.Execution.Pipeline;

/// <summary>
/// 尚未完成 execution 的 StrategyIntent 集合。
///
/// 正式語意：
/// - Strategy 產生 intent 後，不代表立刻成交。
/// - intent 需要等 due time、order latency、後續 market snapshot 才可能成交。
/// - 尚未成交的 intent 必須保留，不能因為某一筆 snapshot 還不能成交就消失。
///
/// 第一版只處理：
/// - add pending intent
/// - 每筆 snapshot 嘗試處理所有 pending intents
/// - 成交的移除
/// - 未成交的保留
///
/// 第一版不處理：
/// - cancel
/// - expire
/// - replace
/// - partial fill
/// - order id lifecycle
/// </summary>
public sealed class PendingIntentBook
{
    private readonly List<StrategyIntent> _pending;
    private readonly List<OrderPlan> _pendingOrderPlans;

    /// <summary>
    /// 建立 pending intent book。
    /// </summary>
    public PendingIntentBook(int capacity = 16)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _pending = new List<StrategyIntent>(capacity);
        _pendingOrderPlans = new List<OrderPlan>(capacity);
    }

    /// <summary>
    /// 目前 pending intent 數量。
    /// </summary>
    public int Count => _pending.Count;

    /// <summary>
    /// 是否沒有 pending intent。
    /// </summary>
    public bool IsEmpty => _pending.Count == 0;

    /// <summary>
    /// 目前 pending intents。
    /// </summary>
    public IReadOnlyList<StrategyIntent> Items => _pending;

    public int PendingOrderPlanCount => _pendingOrderPlans.Count;

    public IReadOnlyList<OrderPlan> OrderPlanItems => _pendingOrderPlans;

    public bool HasPendingExit(
        string symbol,
        DateOnly tradingDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        return _pending.Any(intent =>
            intent.Kind == StrategyIntentKind.Exit
            && string.Equals(intent.Symbol, symbol, StringComparison.Ordinal)
            && intent.TradingDate == tradingDate);
    }

    public long? GetNextSubmittedTimestampUs(
        SimulatedTradePipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        long? next = null;

        foreach (var intent in _pending)
        {
            if (intent.IsCancelPendingEntry)
                continue;

            var submittedTimestampUs = pipeline.GetSubmittedTimestampUs(intent);
            if (!next.HasValue || submittedTimestampUs < next.Value)
                next = submittedTimestampUs;
        }

        return next;
    }

    public long? GetNextDeadlineExitSubmittedTimestampUs(
        SimulatedTradePipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        long? next = null;

        foreach (var intent in _pending)
        {
            if (!IsDeadlineDrivenExit(intent))
                continue;

            var submittedTimestampUs = pipeline.GetSubmittedTimestampUs(intent);
            if (!next.HasValue || submittedTimestampUs < next.Value)
                next = submittedTimestampUs;
        }

        return next;
    }

    /// <summary>
    /// 加入一筆 pending intent。
    /// </summary>
    public void Add(StrategyIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        intent.Validate();

        _pending.Add(intent);
    }

    /// <summary>
    /// 加入多筆 pending intents。
    /// </summary>
    public void AddRange(IEnumerable<StrategyIntent> intents)
    {
        ArgumentNullException.ThrowIfNull(intents);

        foreach (var intent in intents)
        {
            Add(intent);
        }
    }

    /// <summary>
    /// 清空所有 pending intents。
    /// </summary>
    public void Clear()
    {
        _pending.Clear();
        _pendingOrderPlans.Clear();
    }

    public void AddPlan(OrderPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        plan.Validate();

        if (plan.Kind == StrategyIntentKind.Exit
            && HasPendingExitPlan(
                plan.StrategyId,
                plan.Symbol,
                plan.TradingDate,
                plan.EntryCycleId))
        {
            return;
        }

        _pendingOrderPlans.Add(plan);
    }

    public void AddPlans(IEnumerable<OrderPlan> plans)
    {
        ArgumentNullException.ThrowIfNull(plans);

        foreach (var plan in plans)
            AddPlan(plan);
    }

    public long? GetNextOrderPlanSubmittedTimestampUs(
        ExecutionSimulator executionSimulator)
    {
        ArgumentNullException.ThrowIfNull(executionSimulator);

        long? next = null;

        foreach (var plan in _pendingOrderPlans)
        {
            if (plan.Kind == StrategyIntentKind.CancelPendingEntry)
                continue;

            var submittedTimestampUs = executionSimulator.GetSubmittedTimestampUs(plan);
            if (!next.HasValue || submittedTimestampUs < next.Value)
                next = submittedTimestampUs;
        }

        return next;
    }

    public int CancelPendingEntryPlans(
        string strategyId,
        string symbol,
        DateOnly tradingDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        var before = _pendingOrderPlans.Count;

        _pendingOrderPlans.RemoveAll(plan =>
            plan.Kind == StrategyIntentKind.Entry
            && string.Equals(plan.StrategyId, strategyId, StringComparison.Ordinal)
            && string.Equals(plan.Symbol, symbol, StringComparison.Ordinal)
            && plan.TradingDate == tradingDate);

        return before - _pendingOrderPlans.Count;
    }

    public bool HasPendingExitPlan(
        string strategyId,
        string symbol,
        DateOnly tradingDate,
        Guid entryCycleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        return _pendingOrderPlans.Any(plan =>
            plan.Kind == StrategyIntentKind.Exit
            && string.Equals(plan.StrategyId, strategyId, StringComparison.Ordinal)
            && string.Equals(plan.Symbol, symbol, StringComparison.Ordinal)
            && plan.TradingDate == tradingDate
            && plan.EntryCycleId == entryCycleId);
    }

    public int CancelPendingExitPlans(
        string strategyId,
        string symbol,
        DateOnly tradingDate,
        Guid entryCycleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        var before = _pendingOrderPlans.Count;

        _pendingOrderPlans.RemoveAll(plan =>
            plan.Kind == StrategyIntentKind.Exit
            && string.Equals(plan.StrategyId, strategyId, StringComparison.Ordinal)
            && string.Equals(plan.Symbol, symbol, StringComparison.Ordinal)
            && plan.TradingDate == tradingDate
            && plan.EntryCycleId == entryCycleId);

        return before - _pendingOrderPlans.Count;
    }

    public OrderPlanBookProcessResult ProcessOrderPlansAtMarketEvent(
        ExecutionSimulator executionSimulator,
        in MarketSnapshot marketEvent,
        ExecutionFillTimingMode fillTimingMode,
        SimulatedPositionState? currentPosition = null)
    {
        ArgumentNullException.ThrowIfNull(executionSimulator);

        if (_pendingOrderPlans.Count == 0)
            return OrderPlanBookProcessResult.Empty();

        var canceledCount = 0;
        var cancelPlans = _pendingOrderPlans
            .Where(plan => plan.Kind == StrategyIntentKind.CancelPendingEntry)
            .ToArray();

        foreach (var cancelPlan in cancelPlans)
            canceledCount += RemoveMatchingPendingEntryPlans(_pendingOrderPlans, cancelPlan);

        var plansToProcess = _pendingOrderPlans
            .Where(plan => plan.Kind != StrategyIntentKind.CancelPendingEntry)
            .ToArray();
        var results = new List<ExecutionSimulationResult>(plansToProcess.Length);
        var stillPending = new List<OrderPlan>(plansToProcess.Length);
        var closedExitCycles = new HashSet<Guid>();

        foreach (var plan in plansToProcess)
        {
            if (ShouldSkipExitPlan(
                plan,
                currentPosition,
                closedExitCycles))
            {
                continue;
            }

            var submittedTimestampUs = executionSimulator.GetSubmittedTimestampUs(plan);
            if (marketEvent.TimestampUs < submittedTimestampUs)
            {
                results.Add(ExecutionSimulationResult.Rejected(
                    marketEvent.TimestampUs < plan.FirstDueTimestampUs
                        ? ExecutionRejectReason.IntentNotDue
                        : ExecutionRejectReason.OrderLatencyNotReached));
                stillPending.Add(plan);
                continue;
            }

            var result = executionSimulator.TryExecuteAtMarketEvent(
                plan,
                marketEvent,
                fillTimingMode);

            results.Add(result);

            if (result.RejectReason is ExecutionRejectReason.IntentNotDue
                or ExecutionRejectReason.OrderLatencyNotReached)
            {
                stillPending.Add(plan);
            }
            else if (result.Fill?.IntentKind == SimulatedTradeIntentKind.Exit)
            {
                closedExitCycles.Add(result.Fill.EntryCycleId);
            }
        }

        _pendingOrderPlans.Clear();
        _pendingOrderPlans.AddRange(stillPending);

        return new OrderPlanBookProcessResult
        {
            Results = results,
            CanceledPendingEntryCount = canceledCount
        };
    }

    private static int RemoveMatchingPendingEntryPlans(
        List<OrderPlan> plans,
        OrderPlan cancelPlan)
    {
        var before = plans.Count;

        plans.RemoveAll(plan =>
            plan.Kind == StrategyIntentKind.Entry
            && string.Equals(plan.StrategyId, cancelPlan.StrategyId, StringComparison.Ordinal)
            && string.Equals(plan.Symbol, cancelPlan.Symbol, StringComparison.Ordinal)
            && plan.TradingDate == cancelPlan.TradingDate);

        return before - plans.Count;
    }

    private static bool ShouldSkipExitPlan(
        OrderPlan plan,
        SimulatedPositionState? currentPosition,
        HashSet<Guid> closedExitCycles)
    {
        if (plan.Kind != StrategyIntentKind.Exit)
            return false;

        if (closedExitCycles.Contains(plan.EntryCycleId))
            return true;

        if (currentPosition is null)
            return false;

        if (currentPosition.IsFlat)
            return true;

        if (plan.EntryCycleId != Guid.Empty
            && currentPosition.CurrentEntryCycleId != Guid.Empty
            && plan.EntryCycleId != currentPosition.CurrentEntryCycleId)
        {
            return true;
        }

        return false;
    }

    public int CancelPendingEntries(
        string strategyId,
        string symbol,
        DateOnly tradingDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        var before = _pending.Count;

        _pending.RemoveAll(intent =>
            intent.Kind == StrategyIntentKind.Entry
            && string.Equals(intent.StrategyId, strategyId, StringComparison.Ordinal)
            && string.Equals(intent.Symbol, symbol, StringComparison.Ordinal)
            && intent.TradingDate == tradingDate);

        return before - _pending.Count;
    }

    public int CancelPendingExits(
        string strategyId,
        string symbol,
        DateOnly tradingDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        var before = _pending.Count;

        _pending.RemoveAll(intent =>
            intent.Kind == StrategyIntentKind.Exit
            && string.Equals(intent.StrategyId, strategyId, StringComparison.Ordinal)
            && string.Equals(intent.Symbol, symbol, StringComparison.Ordinal)
            && intent.TradingDate == tradingDate);

        return before - _pending.Count;
    }

    /// <summary>
    /// 使用目前 snapshot 嘗試處理所有 pending intents。
    ///
    /// 成交者會從 pending 移除。
    /// 尚未成交者會保留，等待後續 snapshot 再試。
    /// </summary>
    public PendingIntentBookProcessResult ProcessSnapshot(
        SimulatedTradePipeline pipeline,
        in MarketSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        if (_pending.Count == 0)
            return PendingIntentBookProcessResult.Empty();

        var results = new List<SimulatedTradePipelineResult>(_pending.Count);
        var stillPending = new List<StrategyIntent>(_pending.Count);
        var cancelRequests = GetCancelRequests();

        foreach (var intent in _pending)
        {
            if (intent.IsCancelPendingEntry)
                continue;

            if (IsCancelledBy(intent, cancelRequests))
                continue;

            var result = pipeline.TryProcessIntent(
                intent,
                snapshot);

            results.Add(result);

            if (result.IsRejected)
            {
                stillPending.Add(intent);
            }
        }

        _pending.Clear();
        _pending.AddRange(stillPending);

        return new PendingIntentBookProcessResult
        {
            Results = results
        };
    }

    public PendingIntentBookProcessResult ProcessNearestMarketEvent(
        SimulatedTradePipeline pipeline,
        IReadOnlyList<MarketSnapshot> snapshots,
        int currentIndex,
        ExecutionFillTimingMode fillTimingMode)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(snapshots);

        if (_pending.Count == 0)
            return PendingIntentBookProcessResult.Empty();

        if ((uint)currentIndex >= (uint)snapshots.Count)
            throw new ArgumentOutOfRangeException(nameof(currentIndex));

        var currentSnapshot = snapshots[currentIndex];
        var results = new List<SimulatedTradePipelineResult>(_pending.Count);
        var stillPending = new List<StrategyIntent>(_pending.Count);
        var cancelRequests = GetCancelRequests();

        foreach (var intent in _pending)
        {
            if (intent.IsCancelPendingEntry)
                continue;

            if (IsCancelledBy(intent, cancelRequests))
                continue;

            var submittedTimestampUs = pipeline.GetSubmittedTimestampUs(intent);

            if (currentSnapshot.TimestampUs < submittedTimestampUs)
            {
                results.Add(SimulatedTradePipelineResult.Rejected(
                    ExecutionSimulationResult.Rejected(
                        currentSnapshot.TimestampUs < intent.DueTimestampUs
                            ? ExecutionRejectReason.IntentNotDue
                            : ExecutionRejectReason.OrderLatencyNotReached)));
                stillPending.Add(intent);
                continue;
            }

            var marketEvent = ResolveNearestMarketEvent(
                snapshots,
                currentIndex,
                submittedTimestampUs);
            var result = pipeline.TryProcessIntentAtMarketEvent(
                intent,
                marketEvent,
                fillTimingMode);

            results.Add(result);

            if (result.IsRejected)
                stillPending.Add(intent);
        }

        _pending.Clear();
        _pending.AddRange(stillPending);

        return new PendingIntentBookProcessResult
        {
            Results = results
        };
    }

    private static bool IsDeadlineDrivenExit(
        StrategyIntent intent)
    {
        if (intent.Kind != StrategyIntentKind.Exit)
            return false;

        return string.Equals(intent.Code.TriggerDetail, "TimeExitDeadline", StringComparison.Ordinal)
            || string.Equals(intent.Code.TriggerDetail, "FadeTimeExitStartReached", StringComparison.Ordinal)
            || string.Equals(intent.Code.TriggerDetail, "EndOfDayForcedExit", StringComparison.Ordinal);
    }

    public PendingIntentBookProcessResult ProcessDueAtLastKnownMarket(
        SimulatedTradePipeline pipeline,
        in MarketSnapshot lastKnownMarket,
        long timestampUs,
        ExecutionFillTimingMode fillTimingMode)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        if (_pending.Count == 0)
            return PendingIntentBookProcessResult.Empty();

        var marketEvent = lastKnownMarket with
        {
            TimestampUs = timestampUs
        };

        var results = new List<SimulatedTradePipelineResult>(_pending.Count);
        var stillPending = new List<StrategyIntent>(_pending.Count);
        var cancelRequests = GetCancelRequests();

        foreach (var intent in _pending)
        {
            if (intent.IsCancelPendingEntry)
                continue;

            if (IsCancelledBy(intent, cancelRequests))
                continue;

            if (!IsDeadlineDrivenExit(intent))
            {
                stillPending.Add(intent);
                continue;
            }

            var submittedTimestampUs = pipeline.GetSubmittedTimestampUs(intent);
            if (submittedTimestampUs > timestampUs)
            {
                stillPending.Add(intent);
                continue;
            }

            var result = pipeline.TryProcessIntentAtMarketEvent(
                intent,
                marketEvent,
                fillTimingMode);

            results.Add(result);

            if (result.IsRejected)
                stillPending.Add(intent);
        }

        _pending.Clear();
        _pending.AddRange(stillPending);

        return new PendingIntentBookProcessResult
        {
            Results = results
        };
    }

    private StrategyIntent[] GetCancelRequests()
    {
        return _pending
            .Where(intent => intent.IsCancelPendingEntry)
            .ToArray();
    }

    private static bool IsCancelledBy(
        StrategyIntent intent,
        IReadOnlyList<StrategyIntent> cancelRequests)
    {
        if (!intent.IsEntry
            || cancelRequests.Count == 0)
        {
            return false;
        }

        return cancelRequests.Any(cancel =>
            string.Equals(cancel.StrategyId, intent.StrategyId, StringComparison.Ordinal)
            && string.Equals(cancel.Symbol, intent.Symbol, StringComparison.Ordinal)
            && cancel.TradingDate == intent.TradingDate);
    }

    private static MarketSnapshot ResolveNearestMarketEvent(
        IReadOnlyList<MarketSnapshot> snapshots,
        int currentIndex,
        long submittedTimestampUs)
    {
        var next = snapshots[currentIndex];

        if (next.TimestampUs == submittedTimestampUs)
            return next;

        MarketSnapshot? previous = null;

        for (var i = currentIndex - 1; i >= 0; i--)
        {
            if (snapshots[i].TimestampUs <= submittedTimestampUs)
            {
                previous = snapshots[i];
                break;
            }
        }

        if (previous is null)
            return next;

        var previousDistance = Math.Abs(previous.Value.TimestampUs - submittedTimestampUs);
        var nextDistance = Math.Abs(next.TimestampUs - submittedTimestampUs);

        return previousDistance <= nextDistance
            ? previous.Value
            : next;
    }

    public IReadOnlyList<ExecutionSimulationResult> ProcessSnapshot(
        ExecutionSimulator executionSimulator,
        in MarketSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(executionSimulator);

        if (_pending.Count == 0)
            return Array.Empty<ExecutionSimulationResult>();

        var results = new List<ExecutionSimulationResult>(_pending.Count);
        var stillPending = new List<StrategyIntent>(_pending.Count);

        foreach (var intent in _pending)
        {
            var result = executionSimulator.TryExecute(
                intent,
                snapshot);

            results.Add(result);

            if (result.IsRejected)
            {
                stillPending.Add(intent);
            }
        }

        _pending.Clear();
        _pending.AddRange(stillPending);

        return results;
    }

    /// <summary>
    /// 將目前 pending intents 複製成陣列。
    /// </summary>
    public StrategyIntent[] ToArray()
    {
        return _pending.ToArray();
    }
}
