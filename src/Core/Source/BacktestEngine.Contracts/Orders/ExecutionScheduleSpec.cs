namespace BacktestEngine.Contracts.Orders;

/// <summary>
/// 策略進出場分批排程與成交時間模型設定。
///
/// 這個設定只描述 execution schedule，不描述策略訊號條件，
/// 也不描述資金額度或張數上限。
/// </summary>
public sealed class ExecutionScheduleSpec
{
    /// <summary>
    /// 進場是否使用分批。
    /// fade 預設 true；trend 預設 false。
    /// </summary>
    public bool EntryUsesBatch { get; init; } = true;

    /// <summary>
    /// 時間出場是否使用分批。
    /// fade 預設 true；trend 預設 false。
    /// </summary>
    public bool TimeExitUsesBatch { get; init; } = true;

    /// <summary>
    /// 分批間隔秒數。
    /// fade 預設 5 秒。
    /// </summary>
    public int BatchIntervalSeconds { get; init; } = 5;

    /// <summary>
    /// 每批張數。
    /// fade 預設每批 1 張。
    /// trend 預設不分批，所以這個欄位通常不生效。
    /// </summary>
    public int BatchLots { get; init; } = 1;

    /// <summary>
    /// 分批成交時間模型。
    ///
    /// 目前正式通用 runtime 採用 NextTickOnly execution timing。
    /// 這個欄位先保留給 schedule 語義與 legacy fade 對照。
    /// </summary>
    public ExecutionFillTimingMode FillTimingMode { get; init; }
        = ExecutionFillTimingMode.ClockDrivenLastKnown;

    /// <summary>
    /// 分批間隔，單位 microseconds。
    /// </summary>
    public long BatchIntervalUs =>
        (long)BatchIntervalSeconds * 1_000_000L;

    public void Validate()
    {
        if (BatchIntervalSeconds <= 0)
            throw new InvalidOperationException("BatchIntervalSeconds 必須大於 0。");

        if (BatchLots <= 0)
            throw new InvalidOperationException("BatchLots 必須大於 0。");
    }
}
