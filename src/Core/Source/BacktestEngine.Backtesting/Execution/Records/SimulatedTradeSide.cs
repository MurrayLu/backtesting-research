namespace BacktestEngine.Backtesting.Execution.Records;

/// <summary>
/// 模擬成交方向。
/// </summary>
public enum SimulatedTradeSide : byte
{
    /// <summary>
    /// 買進。
    ///
    /// 對多單來說通常是開倉或加倉。
    /// 對空單來說通常是回補、減倉或平倉。
    /// </summary>
    Buy = 1,

    /// <summary>
    /// 賣出。
    ///
    /// 對多單來說通常是賣出、減倉或平倉。
    /// 對空單來說通常是放空開倉或加倉。
    /// </summary>
    Sell = 2
}
