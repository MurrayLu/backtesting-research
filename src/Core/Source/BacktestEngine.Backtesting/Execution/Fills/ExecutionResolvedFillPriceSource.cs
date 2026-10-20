namespace BacktestEngine.Backtesting.Execution.Fills;

public enum ExecutionResolvedFillPriceSource : byte
{
    Unknown = 0,
    Latest = 1,
    Bid = 2,
    Ask = 3,
    LatestFallback = 4,
    BidConservativeLatest = 5,
    AskConservativeLatest = 6,
    HardStopConservativeLatest = 7
}
