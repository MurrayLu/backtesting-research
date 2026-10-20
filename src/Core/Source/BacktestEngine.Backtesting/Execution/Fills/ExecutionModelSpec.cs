namespace BacktestEngine.Backtesting.Execution.Fills;

/// <summary>
/// ExecutionSimulator 設定。
/// </summary>
public sealed class ExecutionModelSpec
{
    /// <summary>
    /// 成交價格來源。
    /// </summary>
    public ExecutionFillPriceSource FillPriceSource { get; init; } =
        ExecutionFillPriceSource.LatestPrice;

    /// <summary>
    /// 下單延遲設定。
    /// </summary>
    public required ExecutionOrderLatencySpec OrderLatency { get; init; }

    /// <summary>
    /// 滑價設定。
    /// </summary>
    public required ExecutionSlippageSpec Slippage { get; init; }

    /// <summary>
    /// 是否允許部分成交。
    ///
    /// 第一版 execution simulator 不支援 partial fill，
    /// 但先把語意放進 spec，避免未來再破壞合約。
    /// </summary>
    public bool AllowPartialFill { get; init; }

    public bool ConservativeBidAskLatestGuard { get; init; }

    /// <summary>
    /// 建立預設台股當沖 execution model。
    /// </summary>
    public static ExecutionModelSpec CreateDefaultTaiwanStockIntraday()
    {
        return new ExecutionModelSpec
        {
            FillPriceSource = ExecutionFillPriceSource.LatestPrice,
            OrderLatency = ExecutionOrderLatencySpec.CreateDefault(),
            Slippage = ExecutionSlippageSpec.None(),
            AllowPartialFill = false,
            ConservativeBidAskLatestGuard = false
        };
    }

    /// <summary>
    /// 驗證設定。
    /// </summary>
    public void Validate()
    {
        if (FillPriceSource is not ExecutionFillPriceSource.LatestPrice
            and not ExecutionFillPriceSource.BidAskWithLatestFallback)
        {
            throw new InvalidOperationException("FillPriceSource 必須是有效值。");
        }

        ArgumentNullException.ThrowIfNull(OrderLatency);
        ArgumentNullException.ThrowIfNull(Slippage);

        OrderLatency.Validate();
        Slippage.Validate();

        if (AllowPartialFill)
            throw new NotSupportedException("第一版 ExecutionSimulator 不支援 partial fill。");
    }
}
