namespace BacktestEngine.Contracts.Signals;

/// <summary>
/// 策略意圖大類。
///
/// 這是 strategy 與 execution 的正式交界語義。
/// 它只描述策略想做的是進場還是出場；
/// 實際成交後對持倉造成 Open / Increase / Reduce / Close / Flip，
/// 由 accounting 層根據成交前後 position 計算。
/// </summary>
public enum StrategyIntentKind : byte
{
    /// <summary>
    /// 未指定。
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 進場或加倉意圖。
    /// </summary>
    Entry = 1,

    /// <summary>
    /// 出場、減倉或平倉意圖。
    /// </summary>
    Exit = 2,

    /// <summary>
    /// 取消尚未成交的進場意圖。
    /// </summary>
    CancelPendingEntry = 3
}
