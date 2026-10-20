using BacktestEngine.Backtesting.Execution.Records;

namespace BacktestEngine.Backtesting.Execution.Costs;

/// <summary>
/// 台股單筆成交費用與證交稅模型。
///
/// 第一版規則：
/// - 手續費買賣都收。
/// - 賣出才收證交稅。
/// - 空單進場是 Sell，因此也收證交稅。
/// - 每筆 fill 單獨 round 到整數 TWD。
/// - 預設使用當沖半稅。
/// </summary>
public sealed class FeeTaxModel
{
    /// <summary>
    /// 使用預設台股當沖費稅設定計算單筆 fill。
    /// </summary>
    public FeeTaxResult CalculateForFill(
        SimulatedTradeSide side,
        int fillPriceE4,
        int lots,
        int lotSizeShares)
    {
        return CalculateForFill(
            FeeTaxSpec.CreateDefaultTaiwanStockIntraday(),
            side,
            fillPriceE4,
            lots,
            lotSizeShares);
    }

    /// <summary>
    /// 計算單筆 fill 的成交金額、手續費、證交稅與現金流。
    /// </summary>
    public FeeTaxResult CalculateForFill(
        FeeTaxSpec spec,
        SimulatedTradeSide side,
        int fillPriceE4,
        int lots,
        int lotSizeShares)
    {
        ArgumentNullException.ThrowIfNull(spec);
        spec.Validate();

        if (side is not SimulatedTradeSide.Buy
            and not SimulatedTradeSide.Sell)
        {
            throw new ArgumentOutOfRangeException(
                nameof(side),
                side,
                "Side 必須是 Buy 或 Sell。");
        }

        if (fillPriceE4 <= 0)
            throw new ArgumentOutOfRangeException(nameof(fillPriceE4), "成交價必須大於 0。");

        if (lots <= 0)
            throw new ArgumentOutOfRangeException(nameof(lots), "成交張數必須大於 0。");

        if (lotSizeShares <= 0)
            throw new ArgumentOutOfRangeException(nameof(lotSizeShares), "每張股數必須大於 0。");

        var shares = (long)lots * lotSizeShares;

        var grossAmountTwd = CalculateGrossAmountTwd(
            fillPriceE4,
            shares);

        var commissionTwd = CalculateCommissionTwd(
            grossAmountTwd,
            spec);

        var taxTwd = side == SimulatedTradeSide.Sell
            ? CalculateSellTaxTwd(grossAmountTwd, spec)
            : 0;

        var netCashFlowTwd = side == SimulatedTradeSide.Buy
            ? -grossAmountTwd - commissionTwd
            : grossAmountTwd - commissionTwd - taxTwd;

        return new FeeTaxResult
        {
            Side = side,
            FillPriceE4 = fillPriceE4,
            Lots = lots,
            LotSizeShares = lotSizeShares,
            GrossAmountTwd = grossAmountTwd,
            CommissionTwd = commissionTwd,
            TaxTwd = taxTwd,
            NetCashFlowTwd = netCashFlowTwd
        };
    }

    /// <summary>
    /// 計算成交金額，單位 TWD。
    ///
    /// priceE4 = price × 10,000。
    /// gross = priceE4 × shares / 10,000。
    ///
    /// 第一版統一 round 到整數 TWD。
    /// </summary>
    public static long CalculateGrossAmountTwd(
        int fillPriceE4,
        long shares)
    {
        if (fillPriceE4 <= 0)
            throw new ArgumentOutOfRangeException(nameof(fillPriceE4), "成交價必須大於 0。");

        if (shares <= 0)
            throw new ArgumentOutOfRangeException(nameof(shares), "股數必須大於 0。");

        var gross = (decimal)fillPriceE4 * shares / 10_000m;

        return RoundTwd(gross);
    }

    /// <summary>
    /// 計算單筆手續費。
    ///
    /// commission =
    ///   grossAmount × BaseCommissionRate × BrokerFeeDiscount
    ///
    /// 然後 round 到 TWD，
    /// 再套用 MinimumCommissionTwd。
    /// </summary>
    public static long CalculateCommissionTwd(
        long grossAmountTwd,
        FeeTaxSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        spec.Validate();

        if (grossAmountTwd <= 0)
            return 0;

        var rawCommission =
            grossAmountTwd *
            spec.BaseCommissionRate *
            spec.BrokerFeeDiscount;

        var rounded = RoundTwd(rawCommission);

        if (spec.MinimumCommissionTwd <= 0)
            return rounded;

        return Math.Max(rounded, spec.MinimumCommissionTwd);
    }

    /// <summary>
    /// 計算賣出證交稅。
    ///
    /// Buy 不應呼叫此方法。
    /// Sell 使用 spec.GetEffectiveSellTaxRate()。
    /// </summary>
    public static long CalculateSellTaxTwd(
        long grossAmountTwd,
        FeeTaxSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        spec.Validate();

        if (grossAmountTwd <= 0)
            return 0;

        var rawTax =
            grossAmountTwd *
            spec.GetEffectiveSellTaxRate();

        return RoundTwd(rawTax);
    }

    /// <summary>
    /// TWD 四捨五入。
    ///
    /// 使用 AwayFromZero，避免 0.5 被銀行家捨入成偶數。
    /// </summary>
    public static long RoundTwd(decimal value)
    {
        return (long)decimal.Round(
            value,
            decimals: 0,
            MidpointRounding.AwayFromZero);
    }
}
