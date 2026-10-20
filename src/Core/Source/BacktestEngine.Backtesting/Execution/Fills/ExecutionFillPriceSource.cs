namespace BacktestEngine.Backtesting.Execution.Fills;

/// <summary>
/// 模擬成交價格來源。
/// </summary>
public enum ExecutionFillPriceSource : byte
{
    /// <summary>
    /// 未指定。
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 使用 MarketSnapshot.LatestPriceE4。
    /// </summary>
    LatestPrice = 1,

    /// <summary>
    /// 市價單語意：
    /// Buy 使用 AskPriceE4；
    /// Sell 使用 BidPriceE4；
    /// 若 bid / ask 不存在，回退使用 LatestPriceE4。
    /// </summary>
    BidAskWithLatestFallback = 2
}
