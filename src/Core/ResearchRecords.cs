namespace BacktestEngine.Research.Contracts;
public readonly record struct ResearchTemporalEnvelope(
    long ObservedAt,
    long AvailableAt,
    long? HorizonEndAt,
    ResearchTemporalRole TemporalRole,
    ResearchTemporalMode TemporalMode)
{
    public bool IsAvailableAt(long timestampUs) =>
        AvailableAt <= timestampUs
        && TemporalRole == ResearchTemporalRole.EventTimeFeature;
}

public sealed record ResearchBarRecord(
    DateOnly TradingDate,
    string Symbol,
    string Timeframe,
    long OpenTimestampUs,
    long CloseTimestampUs,
    int OpenPriceE4,
    int HighPriceE4,
    int LowPriceE4,
    int ClosePriceE4,
    long Volume,
    int? FormalVwapE4,
    int? FormalMa5E4,
    int? FormalMa10E4,
    int? FormalMa20E4,
    string? MissingReason,
    int? FormalMa60E4 = null,
    bool IsDeveloping = false,
    long? AvailableAtUs = null);

