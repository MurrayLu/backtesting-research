namespace BacktestEngine.Backtesting.Execution.Accounting;

/// <summary>
/// 通用模擬持倉狀態。
///
/// 持倉張數採用 signed lots：
/// - 多單為正數
/// - 空單為負數
/// - 空手為 0
///
/// 注意：
/// 這個類別目前只定義資料語義，
/// 下一階段才會由 ExecutionSimulator / Accounting 來更新它。
/// </summary>
public sealed class SimulatedPositionState
{
    /// <summary>
    /// 商品代碼。
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// 交易日。
    /// </summary>
    public DateOnly TradingDate { get; init; }

    /// <summary>
    /// signed position lots。
    ///
    /// - 多單為正數
    /// - 空單為負數
    /// - 空手為 0
    /// </summary>
    public int PositionLots { get; init; }

    /// <summary>
    /// 持倉絕對張數。
    /// </summary>
    public int AbsPositionLots => Math.Abs(PositionLots);

    /// <summary>
    /// 是否空手。
    /// </summary>
    public bool IsFlat => PositionLots == 0;

    /// <summary>
    /// 是否持有多單。
    /// </summary>
    public bool IsLong => PositionLots > 0;

    /// <summary>
    /// 是否持有空單。
    /// </summary>
    public bool IsShort => PositionLots < 0;

    /// <summary>
    /// 持倉成本加總。
    ///
    /// 單位：priceE4 × abs(lots)。
    ///
    /// 注意：
    /// 這裡不使用 signed value。
    /// 不論多單或空單，成本基礎都用正數保存。
    /// 方向由 PositionLots 的正負號表示。
    /// </summary>
    public long PositionEntryValueE4Lots { get; init; }

    public Guid CurrentEntryCycleId { get; init; }

    public long FirstEntryFillTimestampUs { get; init; }

    /// <summary>
    /// 平均進場價 e4。
    /// </summary>
    public int AverageEntryPriceE4
    {
        get
        {
            var absLots = AbsPositionLots;

            if (absLots <= 0)
                return 0;

            return (int)(PositionEntryValueE4Lots / absLots);
        }
    }

    /// <summary>
    /// 已實現毛損益，單位 TWD。
    /// </summary>
    public long RealizedGrossPnlTwd { get; init; }

    /// <summary>
    /// 已累計手續費，單位 TWD。
    /// </summary>
    public long CommissionTwd { get; init; }

    /// <summary>
    /// 已累計證交稅，單位 TWD。
    /// </summary>
    public long TaxTwd { get; init; }

    /// <summary>
    /// 已實現淨損益，單位 TWD。
    /// </summary>
    public long RealizedNetPnlTwd { get; init; }

    /// <summary>
    /// 建立空手狀態。
    /// </summary>
    public static SimulatedPositionState Empty(
        string symbol,
        DateOnly tradingDate)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol 不可為空。", nameof(symbol));

        return new SimulatedPositionState
        {
            Symbol = symbol,
            TradingDate = tradingDate,
            PositionLots = 0,
            PositionEntryValueE4Lots = 0,
            CurrentEntryCycleId = Guid.Empty,
            FirstEntryFillTimestampUs = 0,
            RealizedGrossPnlTwd = 0,
            CommissionTwd = 0,
            TaxTwd = 0,
            RealizedNetPnlTwd = 0
        };
    }
}
