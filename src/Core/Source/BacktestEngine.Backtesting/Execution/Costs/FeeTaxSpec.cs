namespace BacktestEngine.Backtesting.Execution.Costs;

/// <summary>
/// 交易手續費與證交稅設定。
///
/// 這是 execution/accounting profile，
/// 不屬於特定策略。
///
/// 策略只決定是否進出場；
/// 費率、稅率、最低手續費由回測設定或執行環境決定。
/// </summary>
public sealed class FeeTaxSpec
{
    /// <summary>
    /// 台股券商牌告手續費率。
    ///
    /// 預設 0.1425% = 0.001425。
    /// </summary>
    public decimal BaseCommissionRate { get; init; } = 0.001425m;

    /// <summary>
    /// 券商手續費折扣。
    ///
    /// 0.25 代表 0.25 倍，也就是 2.5 折。
    ///
    /// 注意：
    /// 如果你口語說「0.25 折」但實際券商是 0.25 倍，
    /// 這裡用的是 0.25 倍。
    /// </summary>
    public decimal BrokerFeeDiscount { get; init; } = 0.25m;

    /// <summary>
    /// 最低手續費，單位 TWD。
    ///
    /// 第一版預設 20。
    /// 如果想測試理想模型，可以設為 0。
    /// </summary>
    public long MinimumCommissionTwd { get; init; } = 20;

    /// <summary>
    /// 一般股票賣出證交稅率。
    ///
    /// 預設 0.3% = 0.003。
    /// </summary>
    public decimal StockSellTaxRate { get; init; } = 0.003m;

    /// <summary>
    /// 當沖股票賣出證交稅率。
    ///
    /// 預設 0.15% = 0.0015。
    /// </summary>
    public decimal IntradayStockSellTaxRate { get; init; } = 0.0015m;

    /// <summary>
    /// 是否使用當沖半稅。
    ///
    /// 第一版回測預設是當沖，所以預設 true。
    /// </summary>
    public bool UseIntradayReducedTax { get; init; } = true;

    /// <summary>
    /// 建立台股當沖預設費稅設定。
    /// </summary>
    public static FeeTaxSpec CreateDefaultTaiwanStockIntraday()
    {
        return new FeeTaxSpec
        {
            BaseCommissionRate = 0.001425m,
            BrokerFeeDiscount = 0.25m,
            MinimumCommissionTwd = 20,
            StockSellTaxRate = 0.003m,
            IntradayStockSellTaxRate = 0.0015m,
            UseIntradayReducedTax = true
        };
    }

    /// <summary>
    /// 取得實際賣出證交稅率。
    /// </summary>
    public decimal GetEffectiveSellTaxRate()
    {
        return UseIntradayReducedTax
            ? IntradayStockSellTaxRate
            : StockSellTaxRate;
    }

    /// <summary>
    /// 驗證設定。
    /// </summary>
    public void Validate()
    {
        if (BaseCommissionRate < 0)
            throw new InvalidOperationException("BaseCommissionRate 不可小於 0。");

        if (BrokerFeeDiscount < 0)
            throw new InvalidOperationException("BrokerFeeDiscount 不可小於 0。");

        if (MinimumCommissionTwd < 0)
            throw new InvalidOperationException("MinimumCommissionTwd 不可小於 0。");

        if (StockSellTaxRate < 0)
            throw new InvalidOperationException("StockSellTaxRate 不可小於 0。");

        if (IntradayStockSellTaxRate < 0)
            throw new InvalidOperationException("IntradayStockSellTaxRate 不可小於 0。");
    }
}
