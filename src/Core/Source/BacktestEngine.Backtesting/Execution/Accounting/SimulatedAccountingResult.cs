using BacktestEngine.Backtesting.Execution.Records;

namespace BacktestEngine.Backtesting.Execution.Accounting;

/// <summary>
/// 單筆 fill 經 accounting 後的結果。
/// </summary>
public sealed class SimulatedAccountingResult
{
    /// <summary>
    /// 成交前持倉狀態。
    /// </summary>
    public required SimulatedPositionState PositionBefore { get; init; }

    /// <summary>
    /// 成交後持倉狀態。
    /// </summary>
    public required SimulatedPositionState PositionAfter { get; init; }

    /// <summary>
    /// 由本次成交產生的通用交易紀錄。
    /// </summary>
    public required SimulatedTradeRecord Trade { get; init; }
}
