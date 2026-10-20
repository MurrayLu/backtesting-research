namespace BacktestEngine.Backtesting.Execution.Records;

/// <summary>
/// 一筆成交對持倉造成的數學效果。
///
/// 注意：
/// 這不是策略原因。
/// 例如硬停損、衰竭出場、時間出場都不應該放在這裡。
/// 那些應該由 SimulatedTradeRecord.TriggerCode / TriggerGroup 表示。
/// </summary>
public enum SimulatedPositionEffect : byte
{
    /// <summary>
    /// 未指定。
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 從空手建立新倉位。
    /// </summary>
    Open = 1,

    /// <summary>
    /// 在既有同方向倉位上加倉。
    /// </summary>
    Increase = 2,

    /// <summary>
    /// 減少既有倉位，但尚未完全平倉。
    /// </summary>
    Reduce = 3,

    /// <summary>
    /// 完全平掉既有倉位，成交後 position = 0。
    /// </summary>
    Close = 4,

    /// <summary>
    /// 從多翻空或從空翻多。
    ///
    /// 第一版 execution 可以先不主動支援，
    /// 但資料模型保留這個結果語意。
    /// </summary>
    Flip = 5
}
