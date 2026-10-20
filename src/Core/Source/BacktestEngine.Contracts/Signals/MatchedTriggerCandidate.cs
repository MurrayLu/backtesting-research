namespace BacktestEngine.Contracts.Signals;

/// <summary>
/// PriorityPolicy 保留下來的候選訊號摘要。
/// </summary>
public sealed class MatchedTriggerCandidate
{
    public required StrategyIntentCode Code { get; init; }

    public int DecisionPriceE4 { get; init; }

    public IReadOnlyList<TriggerEvidenceItem> Evidence { get; init; } =
        Array.Empty<TriggerEvidenceItem>();
}

/// <summary>
/// 單筆 trigger evidence。
/// </summary>
public sealed class TriggerEvidenceItem
{
    public required string Name { get; init; }

    public string Value { get; init; } = "";

    public string Unit { get; init; } = "";
}
