namespace BacktestEngine.Contracts.Orders;

/// <summary>
/// 分批成交時間模型。
///
/// 這個 enum 是通用 execution 語意，
/// 不屬於特定策略。
/// </summary>
public enum ExecutionFillTimingMode : byte
{
    /// <summary>
    /// 到達下一批預定時間後，等待下一筆市場 snapshot 進來，
    /// 並使用那一筆 snapshot 的價格成交。
    /// </summary>
    TickDrivenNextTick = 1,

    /// <summary>
    /// 到達下一批預定時間時，使用上一筆已知市場 snapshot 的價格成交。
    ///
    /// 這比較接近實盤 simulation：
    /// timer 到點後使用 LatestTickSlot 目前保存的最新價格。
    /// </summary>
    ClockDrivenLastKnown = 2,

    NearestMarketEvent = 3
}
