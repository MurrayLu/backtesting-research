namespace BacktestEngine.Backtesting.Execution.Fills;

/// <summary>
/// ExecutionSimulator 單筆 intent 嘗試執行結果。
/// </summary>
public sealed class ExecutionSimulationResult
{
    /// <summary>
    /// 是否已產生成交。
    /// </summary>
    public bool IsFilled => Fill is not null;

    /// <summary>
    /// 是否未成交。
    /// </summary>
    public bool IsRejected => Fill is null;

    /// <summary>
    /// 未成交原因。
    /// </summary>
    public ExecutionRejectReason RejectReason { get; init; }

    /// <summary>
    /// 成交資料。
    /// </summary>
    public SimulatedFill? Fill { get; init; }

    /// <summary>
    /// 建立成交結果。
    /// </summary>
    public static ExecutionSimulationResult Filled(SimulatedFill fill)
    {
        ArgumentNullException.ThrowIfNull(fill);

        return new ExecutionSimulationResult
        {
            RejectReason = ExecutionRejectReason.None,
            Fill = fill
        };
    }

    /// <summary>
    /// 建立未成交結果。
    /// </summary>
    public static ExecutionSimulationResult Rejected(ExecutionRejectReason reason)
    {
        if (reason == ExecutionRejectReason.None)
            throw new ArgumentException("Rejected result 不可使用 None。", nameof(reason));

        return new ExecutionSimulationResult
        {
            RejectReason = reason,
            Fill = null
        };
    }
}
