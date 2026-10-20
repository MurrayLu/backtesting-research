namespace BacktestEngine.Core.Primitives;

/// <summary>
/// 台股一般股票價格升降單位規則。
///
/// 使用 e4 價格：
/// - 0.01 = 100
/// - 0.05 = 500
/// - 0.1  = 1000
/// - 0.5  = 5000
/// - 1    = 10000
/// - 5    = 50000
///
/// 一般股票六級距：
/// - price < 10        → 0.01
/// - 10 <= price < 50  → 0.05
/// - 50 <= price < 100 → 0.1
/// - 100 <= price < 500 → 0.5
/// - 500 <= price < 1000 → 1
/// - price >= 1000 → 5
/// </summary>
public static class TaiwanStockPriceTickRule
{
    public const int Tick001E4 = 100;
    public const int Tick005E4 = 500;
    public const int Tick01E4 = 1_000;
    public const int Tick05E4 = 5_000;
    public const int Tick1E4 = 10_000;
    public const int Tick5E4 = 50_000;

    private const int Price10E4 = 100_000;
    private const int Price50E4 = 500_000;
    private const int Price100E4 = 1_000_000;
    private const int Price500E4 = 5_000_000;
    private const int Price1000E4 = 10_000_000;

    /// <summary>
    /// 取得指定價格所在級距的升降單位 e4。
    /// </summary>
    public static int GetTickSizeE4(int priceE4)
    {
        if (priceE4 <= 0)
            throw new ArgumentOutOfRangeException(nameof(priceE4), "價格必須大於 0。");

        if (priceE4 < Price10E4)
            return Tick001E4;

        if (priceE4 < Price50E4)
            return Tick005E4;

        if (priceE4 < Price100E4)
            return Tick01E4;

        if (priceE4 < Price500E4)
            return Tick05E4;

        if (priceE4 < Price1000E4)
            return Tick1E4;

        return Tick5E4;
    }

    /// <summary>
    /// 判斷價格是否落在合法台股升降單位上。
    /// </summary>
    public static bool IsValidPriceE4(int priceE4)
    {
        if (priceE4 <= 0)
            return false;

        var tickSizeE4 = GetTickSizeE4(priceE4);

        return priceE4 % tickSizeE4 == 0;
    }

    /// <summary>
    /// 往上移動指定 tick 數。
    ///
    /// 注意：
    /// 這裡逐 tick 移動，是為了正確處理跨級距邊界。
    /// 例如 49.95 往上一 tick 是 50.00；
    /// 再往上一 tick 才是 50.10。
    /// </summary>
    public static int AddTicks(
        int priceE4,
        int ticks)
    {
        if (priceE4 <= 0)
            throw new ArgumentOutOfRangeException(nameof(priceE4), "價格必須大於 0。");

        if (ticks < 0)
            throw new ArgumentOutOfRangeException(nameof(ticks), "ticks 不可小於 0。");

        var result = priceE4;

        for (var i = 0; i < ticks; i++)
        {
            result = checked(result + GetTickSizeE4(result));
        }

        return result;
    }

    public static int MoveUpByTicks(int priceE4, int ticks) =>
        MoveByTicks(priceE4, ticks, upward: true);

    /// <summary>
    /// 往下移動指定 tick 數。
    ///
    /// 注意：
    /// 往下時要用「目前價格下方一點點」所在級距判斷 tick。
    /// 例如 50.00 往下一 tick 應該是 49.95，不是 49.90。
    /// </summary>
    public static int SubtractTicks(
        int priceE4,
        int ticks)
    {
        if (priceE4 <= 0)
            throw new ArgumentOutOfRangeException(nameof(priceE4), "價格必須大於 0。");

        if (ticks < 0)
            throw new ArgumentOutOfRangeException(nameof(ticks), "ticks 不可小於 0。");

        var result = priceE4;

        for (var i = 0; i < ticks; i++)
        {
            var probeBelow = Math.Max(1, result - 1);
            var tickSizeE4 = GetTickSizeE4(probeBelow);

            result = checked(result - tickSizeE4);

            if (result <= 0)
                throw new InvalidOperationException("價格往下移動後不可小於等於 0。");
        }

        return result;
    }

    public static int MoveDownByTicks(int priceE4, int ticks) =>
        MoveByTicks(priceE4, ticks, upward: false);

    public static int CeilingToValidPriceE4(int priceE4)
    {
        EnsurePositive(priceE4);
        var tick = GetTickSizeE4(priceE4);
        var quotient = (long)priceE4 / tick;
        var remainder = (long)priceE4 % tick;
        if (remainder != 0) quotient++;
        return checked((int)(quotient * tick));
    }

    public static int FloorToValidPriceE4(int priceE4)
    {
        EnsurePositive(priceE4);
        var tick = GetTickSizeE4(priceE4);
        var quotient = (long)priceE4 / tick;
        return checked((int)(quotient * tick));
    }

    public static int NextValidPriceAboveE4(int priceE4)
    {
        EnsurePositive(priceE4);
        var ceiling = CeilingToValidPriceE4(priceE4);
        return ceiling > priceE4 ? ceiling : AddTicks(ceiling, 1);
    }

    public static int NextValidPriceBelowE4(int priceE4)
    {
        EnsurePositive(priceE4);
        var floor = FloorToValidPriceE4(priceE4);
        return floor < priceE4 ? floor : SubtractTicks(floor, 1);
    }

    private static int MoveByTicks(int priceE4, int ticks, bool upward)
    {
        EnsurePositive(priceE4);
        if (ticks < 0)
            throw new ArgumentOutOfRangeException(nameof(ticks));

        var result = priceE4;
        for (var i = 0; i < ticks; i++)
            result = upward
                ? NextValidPriceAboveE4(result)
                : NextValidPriceBelowE4(result);
        return result;
    }

    private static void EnsurePositive(int priceE4)
    {
        if (priceE4 <= 0)
            throw new ArgumentOutOfRangeException(nameof(priceE4));
    }
}
