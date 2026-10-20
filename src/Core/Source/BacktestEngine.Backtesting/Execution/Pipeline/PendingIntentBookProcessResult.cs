using BacktestEngine.Backtesting.Execution.Records;

namespace BacktestEngine.Backtesting.Execution.Pipeline;

/// <summary>
/// PendingIntentBook 對單一 market snapshot 處理後的結果。
/// </summary>
public sealed class PendingIntentBookProcessResult
{
    /// <summary>
    /// 本次 snapshot 嘗試處理的所有 intent 結果。
    /// 包含已成交與尚未成交。
    /// </summary>
    public required IReadOnlyList<SimulatedTradePipelineResult> Results { get; init; }

    /// <summary>
    /// 本次成交結果。
    /// </summary>
    public IReadOnlyList<SimulatedTradePipelineResult> FilledResults =>
        Results.Where(x => x.IsFilled).ToArray();

    /// <summary>
    /// 本次尚未成交結果。
    /// </summary>
    public IReadOnlyList<SimulatedTradePipelineResult> RejectedResults =>
        Results.Where(x => x.IsRejected).ToArray();

    /// <summary>
    /// 本次產生的交易紀錄。
    /// </summary>
    public IReadOnlyList<SimulatedTradeRecord> Trades =>
        Results
            .Where(x => x.Trade is not null)
            .Select(x => x.Trade!)
            .ToArray();

    /// <summary>
    /// 本次嘗試處理的筆數。
    /// </summary>
    public int AttemptedCount => Results.Count;

    /// <summary>
    /// 本次成交筆數。
    /// </summary>
    public int FilledCount => Results.Count(x => x.IsFilled);

    /// <summary>
    /// 本次尚未成交筆數。
    /// </summary>
    public int RejectedCount => Results.Count(x => x.IsRejected);

    /// <summary>
    /// 是否沒有處理任何 intent。
    /// </summary>
    public bool IsEmpty => Results.Count == 0;

    /// <summary>
    /// 建立空結果。
    /// </summary>
    public static PendingIntentBookProcessResult Empty()
    {
        return new PendingIntentBookProcessResult
        {
            Results = Array.Empty<SimulatedTradePipelineResult>()
        };
    }
}
