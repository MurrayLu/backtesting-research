namespace BacktestEngine.Backtesting.Execution.Accounting;

public sealed class SimulatedAccountingIssue
{
    public SimulatedAccountingIssueReason Reason { get; init; }

    public string Message { get; init; } = "";

    public Guid EntryCycleId { get; init; }
}

public enum SimulatedAccountingIssueReason : byte
{
    None = 0,
    ActiveEntryCycleExists = 1,
    EntryCycleMismatch = 2,
    UnsupportedConcurrentEntryCycle = 3,
    NoOpenPosition = 4,
    CloseLotsExceedOpenLots = 5,
    UnsupportedFlip = 6
}
