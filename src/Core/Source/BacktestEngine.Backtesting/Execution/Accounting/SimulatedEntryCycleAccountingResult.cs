using BacktestEngine.Backtesting.Execution.Records;

namespace BacktestEngine.Backtesting.Execution.Accounting;

public sealed class SimulatedEntryCycleAccountingResult
{
    public required IReadOnlyList<SimulatedFillLedgerRecord> FillLedgerRecords { get; init; }

    public required IReadOnlyList<CompletedTradeRecord> CompletedTradeRecords { get; init; }

    public required IReadOnlyList<SimulatedPositionState> OpenPositions { get; init; }

    public required IReadOnlyList<SimulatedAccountingIssue> AccountingIssues { get; init; }

    public long TotalRealizedGrossPnlTwd =>
        CompletedTradeRecords.Sum(x => x.RealizedGrossPnlTwd);

    public long TotalCommissionTwd =>
        CompletedTradeRecords.Sum(x => x.TotalCommissionTwd);

    public long TotalTransactionTaxTwd =>
        CompletedTradeRecords.Sum(x => x.TotalTransactionTaxTwd);

    public long TotalRealizedNetPnlTwd =>
        CompletedTradeRecords.Sum(x => x.RealizedNetPnlTwd);

    public bool HasOpenPosition => OpenPositions.Count > 0;
}
