using BacktestEngine.Backtesting.Execution.Records;

namespace BacktestEngine.Backtesting.Execution.Costs;

/// <summary>
/// 單筆 fill 的費用與稅計算結果。
/// </summary>
public sealed class FeeTaxResult
{
    /// <summary>
    /// 成交方向。
    /// </summary>
    public SimulatedTradeSide Side { get; init; }

    /// <summary>
    /// 成交價 e4。
    /// </summary>
    public int FillPriceE4 { get; init; }

    /// <summary>
    /// 成交張數。
    /// </summary>
    public int Lots { get; init; }

    /// <summary>
    /// 每張股數。
    /// </summary>
    public int LotSizeShares { get; init; }

    /// <summary>
    /// 成交股數。
    /// </summary>
    public long Shares => (long)Lots * LotSizeShares;

    /// <summary>
    /// 成交金額，單位 TWD。
    ///
    /// GrossAmountTwd = FillPrice × Shares。
    /// </summary>
    public long GrossAmountTwd { get; init; }

    /// <summary>
    /// 券商手續費，單位 TWD。
    /// </summary>
    public long CommissionTwd { get; init; }

    /// <summary>
    /// 證交稅，單位 TWD。
    ///
    /// Buy = 0。
    /// Sell = GrossAmount × effective sell tax rate。
    /// </summary>
    public long TaxTwd { get; init; }

    /// <summary>
    /// 總交易成本，單位 TWD。
    ///
    /// TotalCostTwd = CommissionTwd + TaxTwd。
    /// </summary>
    public long TotalCostTwd => CommissionTwd + TaxTwd;

    /// <summary>
    /// 這筆成交對現金造成的影響，單位 TWD。
    ///
    /// Buy:
    ///   -GrossAmountTwd - CommissionTwd
    ///
    /// Sell:
    ///   +GrossAmountTwd - CommissionTwd - TaxTwd
    /// </summary>
    public long NetCashFlowTwd { get; init; }
}
