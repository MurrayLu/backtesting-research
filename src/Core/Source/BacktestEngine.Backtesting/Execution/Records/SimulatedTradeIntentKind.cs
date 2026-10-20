namespace BacktestEngine.Backtesting.Execution.Records;

/// <summary>
/// 模擬成交的策略意圖大類。
///
/// 這只描述通用 Entry / Exit，
/// 不描述策略內部細分原因。
///
/// 策略細分原因請使用：
/// - TriggerId
/// - TriggerCode
/// - TriggerGroup
/// - TriggerName
/// - TriggerDetail
/// </summary>
public enum SimulatedTradeIntentKind : byte
{
    /// <summary>
    /// 未指定。
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 進場或加倉。
    /// </summary>
    Entry = 1,

    /// <summary>
    /// 出場、減倉或平倉。
    /// </summary>
    Exit = 2
}
