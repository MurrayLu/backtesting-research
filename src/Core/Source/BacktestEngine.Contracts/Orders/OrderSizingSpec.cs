namespace BacktestEngine.Contracts.Orders;

/// <summary>
/// 下單張數計算設定。
///
/// 這個設定只回答一件事：
/// 「每檔給定多少金額，在某個價格下最多可以下幾張？」
///
/// 策略本身不應該直接寫死 20 萬、500 萬、1000 股。
/// 這樣未來回測、模擬單、實單才可以共用同一套 sizing 規則。
/// </summary>
public sealed class OrderSizingSpec
{
    /// <summary>
    /// 每檔可使用金額，單位 TWD。
    /// </summary>
    public long PerSymbolBudgetTwd { get; init; }

    /// <summary>
    /// 每張股數。
    ///
    /// 台股一般股票一張為 1000 股。
    /// </summary>
    public int LotSizeShares { get; init; } = 1000;

    /// <summary>
    /// 最少要能下幾張才允許進場。
    ///
    /// 第一版使用整張交易，不做零股。
    /// </summary>
    public int MinimumLots { get; init; } = 1;

    /// <summary>
    /// 是否允許零股。
    ///
    /// 第一版固定 false。
    /// </summary>
    public bool AllowOddLot { get; init; } = false;

    /// <summary>
    /// 如果預算不足以買一張，是否略過。
    ///
    /// 第一版固定 true。
    /// </summary>
    public bool SkipIfBudgetCannotBuyOneLot { get; init; } = true;

}
