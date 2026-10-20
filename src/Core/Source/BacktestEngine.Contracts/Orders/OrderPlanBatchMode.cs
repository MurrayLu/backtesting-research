namespace BacktestEngine.Contracts.Orders;

/// <summary>
/// OrderPlan 轉成 StrategyIntent 時的分批模式。
/// </summary>
public enum OrderPlanBatchMode : byte
{
    /// <summary>
    /// 使用 ExecutionScheduleSpec 的預設規則。
    ///
    /// Entry 使用 EntryUsesBatch。
    /// Exit 使用 TimeExitUsesBatch。
    /// </summary>
    UseScheduleDefault = 0,

    /// <summary>
    /// 強制單筆送出。
    ///
    /// 適合 hard stop / forced flatten 這類必須一次處理的風控單。
    /// </summary>
    ForceSingle = 1,

    /// <summary>
    /// 強制分批送出。
    ///
    /// 適合策略明確要求分批，不想依賴 schedule 的 entry / exit 預設值。
    /// </summary>
    ForceBatch = 2
}
