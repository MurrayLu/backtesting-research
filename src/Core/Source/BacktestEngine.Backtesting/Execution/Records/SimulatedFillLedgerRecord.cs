using BacktestEngine.Contracts.Signals;

namespace BacktestEngine.Backtesting.Execution.Records;

public sealed class SimulatedFillLedgerRecord
{
    public required string StrategyId { get; init; }

    public string StrategyRunId { get; init; } = "";

    public required string Symbol { get; init; }

    public DateOnly TradingDate { get; init; }

    public Guid EntryCycleId { get; init; }

    public Guid CorrelationId { get; init; }

    public long FillId { get; init; }

    public SimulatedTradeSide Side { get; init; }

    public SimulatedTradeIntentKind IntentKind { get; init; }

    public int Lots { get; init; }

    public int LotSizeShares { get; init; }

    public long Shares => (long)Lots * LotSizeShares;

    public int FillPriceE4 { get; init; }

    public long FillTimestampUs { get; init; }

    public long GrossNotionalTwd { get; init; }

    public SimulatedPositionEffect PositionEffect { get; init; }

    public int PositionLotsAfter { get; init; }

    public int AverageEntryPriceE4After { get; init; }

    public long RealizedGrossPnlTwd { get; init; }

    public long CommissionTwd { get; init; }

    public long TransactionTaxTwd { get; init; }

    public long NetRealizedPnlTwd { get; init; }

    public string TriggerCode { get; init; } = "";

    public string TriggerDetailCode { get; init; } = "";

    public IReadOnlyList<TriggerEvidenceItem> TriggerEvidence { get; init; } =
        Array.Empty<TriggerEvidenceItem>();

    public IReadOnlyList<TriggerEvidenceItem> EntryTriggerEvidence { get; init; } =
        Array.Empty<TriggerEvidenceItem>();
}
