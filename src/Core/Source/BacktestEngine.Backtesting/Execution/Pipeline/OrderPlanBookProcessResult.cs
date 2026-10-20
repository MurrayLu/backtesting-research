using BacktestEngine.Backtesting.Execution.Fills;

namespace BacktestEngine.Backtesting.Execution.Pipeline;

public sealed class OrderPlanBookProcessResult
{
    public required IReadOnlyList<ExecutionSimulationResult> Results { get; init; }

    public int CanceledPendingEntryCount { get; init; }

    public bool IsEmpty => Results.Count == 0 && CanceledPendingEntryCount == 0;

    public IReadOnlyList<SimulatedFill> Fills =>
        Results
            .Where(x => x.Fill is not null)
            .Select(x => x.Fill!)
            .ToArray();

    public IReadOnlyList<ExecutionSimulationResult> Rejects =>
        Results
            .Where(x => x.IsRejected)
            .ToArray();

    public static OrderPlanBookProcessResult Empty()
    {
        return new OrderPlanBookProcessResult
        {
            Results = Array.Empty<ExecutionSimulationResult>(),
            CanceledPendingEntryCount = 0
        };
    }
}
