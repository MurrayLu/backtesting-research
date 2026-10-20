namespace BacktestEngine.Backtesting.Execution.Fills;

/// <summary>
/// 模擬下單延遲設定。
/// </summary>
public sealed class ExecutionOrderLatencySpec
{
    /// <summary>
    /// 固定下單延遲，單位 microseconds。
    ///
    /// 預設 100ms = 100,000us。
    /// </summary>
    public long FixedLatencyUs { get; init; } = 100_000;

    /// <summary>
    /// 建立預設下單延遲設定。
    /// </summary>
    public static ExecutionOrderLatencySpec CreateDefault()
    {
        return new ExecutionOrderLatencySpec
        {
            FixedLatencyUs = 100_000
        };
    }

    /// <summary>
    /// 驗證設定。
    /// </summary>
    public void Validate()
    {
        if (FixedLatencyUs < 0)
            throw new InvalidOperationException("FixedLatencyUs 不可小於 0。");
    }
}
