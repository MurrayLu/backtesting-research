using BacktestEngine.Backtesting.Execution.Accounting;
using BacktestEngine.Backtesting.Execution.Fills;
using BacktestEngine.Backtesting.Execution.Records;

namespace BacktestEngine.Backtesting.Execution.Pipeline;

/// <summary>
/// StrategyIntent 經 execution / accounting pipeline 處理後的結果。
///
/// 如果 execution 尚未成交，只會有 ExecutionResult。
/// 如果 execution 成交，會同時有 AccountingResult 與 SimulatedTradeRecord。
/// </summary>
public sealed class SimulatedTradePipelineResult
{
    /// <summary>
    /// ExecutionSimulator 的結果。
    /// </summary>
    public required ExecutionSimulationResult ExecutionResult { get; init; }

    /// <summary>
    /// AccountingEngine 的結果。
    ///
    /// 只有成交時才會有值。
    /// </summary>
    public SimulatedAccountingResult? AccountingResult { get; init; }

    /// <summary>
    /// 是否已成交並完成 accounting。
    /// </summary>
    public bool IsFilled => AccountingResult is not null;

    /// <summary>
    /// 是否尚未成交。
    /// </summary>
    public bool IsRejected => AccountingResult is null;

    /// <summary>
    /// 未成交原因。
    /// </summary>
    public ExecutionRejectReason RejectReason => ExecutionResult.RejectReason;

    /// <summary>
    /// 成交 fill。
    /// </summary>
    public SimulatedFill? Fill => ExecutionResult.Fill;

    /// <summary>
    /// 本次成交產生的通用交易紀錄。
    /// </summary>
    public SimulatedTradeRecord? Trade => AccountingResult?.Trade;

    /// <summary>
    /// 成交後持倉。
    /// </summary>
    public SimulatedPositionState? PositionAfter => AccountingResult?.PositionAfter;

    /// <summary>
    /// 建立未成交結果。
    /// </summary>
    public static SimulatedTradePipelineResult Rejected(
        ExecutionSimulationResult executionResult)
    {
        ArgumentNullException.ThrowIfNull(executionResult);

        if (executionResult.IsFilled)
            throw new ArgumentException("Rejected pipeline result 不可使用 filled execution result。", nameof(executionResult));

        return new SimulatedTradePipelineResult
        {
            ExecutionResult = executionResult,
            AccountingResult = null
        };
    }

    /// <summary>
    /// 建立成交結果。
    /// </summary>
    public static SimulatedTradePipelineResult Filled(
        ExecutionSimulationResult executionResult,
        SimulatedAccountingResult accountingResult)
    {
        ArgumentNullException.ThrowIfNull(executionResult);
        ArgumentNullException.ThrowIfNull(accountingResult);

        if (!executionResult.IsFilled)
            throw new ArgumentException("Filled pipeline result 必須使用 filled execution result。", nameof(executionResult));

        return new SimulatedTradePipelineResult
        {
            ExecutionResult = executionResult,
            AccountingResult = accountingResult
        };
    }
}
