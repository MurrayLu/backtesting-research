namespace BacktestEngine.Contracts.Signals;

/// <summary>
/// Strategy V2 在 strategy/package 邊界描述的數量語意。
/// 實際張數仍由後續 sizing / execution 層決定。
/// </summary>
public enum StrategyQuantityIntent : byte
{
    Unknown = 0,
    OpenBySizingProfile = 1,
    CloseAll = 2,
    CancelPendingEntry = 3
}
