using BacktestEngine.Core.Primitives;
using BacktestEngine.Contracts.Common;

namespace BacktestEngine.Backtesting.Execution.Fills;

/// <summary>
/// 模擬滑價設定。
///
/// 第一版使用固定 tick 數，tick size 使用台股一般股票升降單位。
/// Buy 會往上滑，Sell 會往下滑。
/// </summary>
public sealed class ExecutionSlippageSpec
{
    /// <summary>
    /// 固定滑價 tick 數。
    ///
    /// 0 代表不滑價。
    /// </summary>
    public int FixedSlippageTicks { get; init; }

    /// <summary>
    /// 建立無滑價設定。
    /// </summary>
    public static ExecutionSlippageSpec None()
    {
        return new ExecutionSlippageSpec
        {
            FixedSlippageTicks = 0
        };
    }

    /// <summary>
    /// 驗證設定。
    /// </summary>
    public void Validate()
    {
        if (FixedSlippageTicks < 0)
            throw new InvalidOperationException("FixedSlippageTicks 不可小於 0。");
    }

    /// <summary>
    /// 計算買進滑價後價格。
    /// </summary>
    public int ApplyBuySlippage(int rawFillPriceE4)
    {
        if (rawFillPriceE4 <= 0)
            throw new ArgumentOutOfRangeException(nameof(rawFillPriceE4), "成交價必須大於 0。");

        return TaiwanStockPriceTickRule.AddTicks(
            rawFillPriceE4,
            FixedSlippageTicks);
    }

    /// <summary>
    /// 計算賣出滑價後價格。
    /// </summary>
    public int ApplySellSlippage(int rawFillPriceE4)
    {
        if (rawFillPriceE4 <= 0)
            throw new ArgumentOutOfRangeException(nameof(rawFillPriceE4), "成交價必須大於 0。");

        return TaiwanStockPriceTickRule.SubtractTicks(
            rawFillPriceE4,
            FixedSlippageTicks);
    }
}
