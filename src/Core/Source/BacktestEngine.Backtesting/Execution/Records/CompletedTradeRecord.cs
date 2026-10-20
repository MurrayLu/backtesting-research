using BacktestEngine.Contracts.Signals;

namespace BacktestEngine.Backtesting.Execution.Records;

public sealed class CompletedTradeRecord
{
    public required string StrategyId { get; init; }

    public string StrategyRunId { get; init; } = "";

    public required string Symbol { get; init; }

    public DateOnly TradingDate { get; init; }

    public Guid EntryCycleId { get; init; }

    public string Direction { get; init; } = "";

    public long EntryStartedAtTimestampUs { get; init; }

    public long ExitCompletedAtTimestampUs { get; init; }

    public long FirstEntryFillTimestampUs { get; init; }

    public long LastExitFillTimestampUs { get; init; }

    public SimulatedTradeSide EntrySide { get; init; }

    public SimulatedTradeSide ExitSide { get; init; }

    public int TotalEntryLots { get; init; }

    public int TotalExitLots { get; init; }

    public long TotalEntryShares { get; init; }

    public long TotalExitShares { get; init; }

    public int AverageEntryPriceE4 { get; init; }

    public int AverageExitPriceE4 { get; init; }

    public long EntryNotionalTwd { get; init; }

    public long ExitNotionalTwd { get; init; }

    public long RealizedGrossPnlTwd { get; init; }

    public long TotalCommissionTwd { get; init; }

    public long TotalTransactionTaxTwd { get; init; }

    public long RealizedNetPnlTwd { get; init; }

    public string EntryTriggerCode { get; init; } = "";

    public string EntryTriggerDetailCode { get; init; } = "";

    public string ExitTriggerCode { get; init; } = "";

    public string ExitTriggerDetailCode { get; init; } = "";

    public Guid EntryCorrelationId { get; init; }

    public Guid ExitCorrelationId { get; init; }

    public int EntryFillCount { get; init; }

    public int ExitFillCount { get; init; }

    public IReadOnlyList<TriggerEvidenceItem> EntryTriggerEvidence { get; init; } =
        Array.Empty<TriggerEvidenceItem>();
}
