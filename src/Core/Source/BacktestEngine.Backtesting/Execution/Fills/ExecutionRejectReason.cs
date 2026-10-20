namespace BacktestEngine.Backtesting.Execution.Fills;

/// <summary>
/// ExecutionSimulator 拒絕或尚未成交原因。
/// </summary>
public enum ExecutionRejectReason : byte
{
    /// <summary>
    /// 無。
    /// </summary>
    None = 0,

    /// <summary>
    /// intent 尚未到預定執行時間。
    /// </summary>
    IntentNotDue = 1,

    /// <summary>
    /// intent 已到 due time，但尚未到 submitted time。
    /// </summary>
    OrderLatencyNotReached = 2,

    /// <summary>
    /// 訂單已到達市場時間，但目前 snapshot timestamp 剛好等於 arrival time。
    ///
    /// 為避免同 timestamp 無法判斷先後順序，
    /// NextTickOnly 模型要求必須等待 timestamp > arrival time 的下一筆 snapshot。
    /// </summary>
    WaitingNextSnapshotAfterOrderArrival = 3,

    /// <summary>
    /// snapshot 沒有可用成交價格。
    /// </summary>
    NoFillPrice = 4,

    MissingAskPrice = 5,

    MissingBidPrice = 6,

    CancelOrderDoesNotFill = 7
}
