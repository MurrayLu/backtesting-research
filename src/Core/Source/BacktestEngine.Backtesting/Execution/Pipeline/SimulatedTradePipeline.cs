using BacktestEngine.Backtesting.Execution.Accounting;
using BacktestEngine.Backtesting.Execution.Fills;
using BacktestEngine.Contracts.Common;
using BacktestEngine.Contracts.Orders;
using BacktestEngine.Contracts.Signals;

namespace BacktestEngine.Backtesting.Execution.Pipeline;

/// <summary>
/// 正式模擬交易主線。
///
/// 職責：
/// - 接收 StrategyIntent。
/// - 使用 ExecutionSimulator 轉成 SimulatedFill。
/// - 使用 SimulatedAccountingEngine 更新 position。
/// - 產生 SimulatedTradeRecord。
///
/// 不負責：
/// - 策略判斷。
/// - Feature / KLine / VWAP / SMA。
/// - Pending intent lifecycle。
/// - 報表輸出。
///
/// 注意：
/// 如果某個 intent 尚未成交，呼叫端需要保留該 intent，
/// 並在後續 snapshot 再次呼叫 TryProcessIntent。
/// </summary>
public sealed class SimulatedTradePipeline
{
    private readonly ExecutionSimulator _executionSimulator;
    private readonly SimulatedAccountingEngine _accountingEngine;
    private readonly Dictionary<PositionKey, SimulatedPositionState> _positions = new();

    /// <summary>
    /// 使用預設 execution 與 accounting 設定。
    /// </summary>
    public SimulatedTradePipeline()
        : this(
            new ExecutionSimulator(),
            new SimulatedAccountingEngine())
    {
    }

    /// <summary>
    /// 使用指定 execution 與 accounting 元件。
    /// </summary>
    public SimulatedTradePipeline(
        ExecutionSimulator executionSimulator,
        SimulatedAccountingEngine accountingEngine)
    {
        _executionSimulator =
            executionSimulator ?? throw new ArgumentNullException(nameof(executionSimulator));

        _accountingEngine =
            accountingEngine ?? throw new ArgumentNullException(nameof(accountingEngine));
    }

    /// <summary>
    /// 嘗試處理一筆策略意圖。
    ///
    /// 若 execution 尚未成交，不會更新持倉。
    /// 若 execution 成交，會立即套用 accounting 並更新內部持倉。
    /// </summary>
    public SimulatedTradePipelineResult TryProcessIntent(
        StrategyIntent intent,
        in MarketSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(intent);
        intent.Validate();

        var executionResult = _executionSimulator.TryExecute(
            intent,
            snapshot);

        if (!executionResult.IsFilled)
            return SimulatedTradePipelineResult.Rejected(executionResult);

        var fill = executionResult.Fill
            ?? throw new InvalidOperationException("ExecutionResult.IsFilled 為 true，但 Fill 為 null。");

        var key = PositionKey.FromFill(fill);

        var positionBefore = _positions.TryGetValue(
            key,
            out var existingPosition)
            ? existingPosition
            : SimulatedPositionState.Empty(fill.Symbol, fill.TradingDate);

        var accountingResult = _accountingEngine.ApplyFill(
            positionBefore,
            fill);

        _positions[key] = accountingResult.PositionAfter;

        return SimulatedTradePipelineResult.Filled(
            executionResult,
            accountingResult);
    }

    public SimulatedTradePipelineResult TryProcessIntentAtMarketEvent(
        StrategyIntent intent,
        in MarketSnapshot marketEvent,
        ExecutionFillTimingMode fillTimingMode)
    {
        ArgumentNullException.ThrowIfNull(intent);
        intent.Validate();

        var executionResult = _executionSimulator.TryExecuteAtMarketEvent(
            intent,
            marketEvent,
            fillTimingMode);

        if (!executionResult.IsFilled)
            return SimulatedTradePipelineResult.Rejected(executionResult);

        var fill = executionResult.Fill
            ?? throw new InvalidOperationException("ExecutionResult.IsFilled was true but Fill was null.");

        var key = PositionKey.FromFill(fill);

        var positionBefore = _positions.TryGetValue(
            key,
            out var existingPosition)
            ? existingPosition
            : SimulatedPositionState.Empty(fill.Symbol, fill.TradingDate);

        var accountingResult = _accountingEngine.ApplyFill(
            positionBefore,
            fill);

        _positions[key] = accountingResult.PositionAfter;

        return SimulatedTradePipelineResult.Filled(
            executionResult,
            accountingResult);
    }

    public long GetSubmittedTimestampUs(
        StrategyIntent intent)
    {
        return _executionSimulator.GetSubmittedTimestampUs(intent);
    }

    /// <summary>
    /// 取得指定商品與交易日目前持倉。
    /// 若尚未有任何成交，回傳空手狀態。
    /// </summary>
    public SimulatedPositionState GetPosition(
        string symbol,
        DateOnly tradingDate)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol 不可為空。", nameof(symbol));

        var key = new PositionKey(
            symbol,
            tradingDate);

        return _positions.TryGetValue(
            key,
            out var position)
            ? position
            : SimulatedPositionState.Empty(symbol, tradingDate);
    }

    /// <summary>
    /// 取得目前所有已建立過的持倉狀態。
    /// </summary>
    public IReadOnlyList<SimulatedPositionState> SnapshotPositions()
    {
        return _positions
            .Values
            .OrderBy(x => x.TradingDate)
            .ThenBy(x => x.Symbol, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// 清空所有持倉。
    /// </summary>
    public void Clear()
    {
        _positions.Clear();
    }

    private readonly record struct PositionKey(
        string Symbol,
        DateOnly TradingDate)
    {
        public static PositionKey FromFill(SimulatedFill fill)
        {
            ArgumentNullException.ThrowIfNull(fill);

            return new PositionKey(
                fill.Symbol,
                fill.TradingDate);
        }
    }
}
