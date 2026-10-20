namespace BacktestEngine.Contracts.Signals;

/// <summary>
/// 策略意圖方向。
///
/// 注意：
/// Core 專案不能依賴 Backtesting.Execution 的 SimulatedTradeSide，
/// 所以 strategy intent 使用自己的 side enum。
///
/// Backtesting.Execution 之後會負責把 StrategyIntentSide
/// 映射成 SimulatedTradeSide。
/// </summary>
public enum StrategyIntentSide : byte
{
    /// <summary>
    /// 未指定。
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 買進。
    /// </summary>
    Buy = 1,

    /// <summary>
    /// 賣出。
    /// </summary>
    Sell = 2
}
