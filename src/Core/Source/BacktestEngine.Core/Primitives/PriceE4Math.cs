namespace BacktestEngine.Core.Primitives;

/// <summary>
/// 價格 e4 工具。
///
/// e4 代表「價格 × 10,000」。
///
/// 範例：
/// - 66.6  → 666000
/// - 70    → 700000
/// - 225.5 → 2255000
///
/// 為什麼要用 e4：
/// - 策略熱路徑避免 decimal / double。
/// - 價格比較、停損、漲跌幅都可以用 int / long 計算。
/// - 回測與未來實盤策略核心可以共用同一種資料格式。
/// </summary>
public static class PriceE4Math
{
    private const int Scale = 10_000;

    /// <summary>
    /// 將 CSV 內的價格文字轉成 e4 整數。
    ///
    /// 不使用 decimal.Parse，避免在大量 Tick 讀取時產生額外成本。
    ///
    /// 支援格式：
    /// - "70"
    /// - "66.6"
    /// - "66.65"
    /// - "66.6666"
    ///
    /// 如果小數超過 4 位，會用第 5 位做四捨五入。
    /// </summary>
    public static int ParsePriceE4(ReadOnlySpan<char> text)
    {
        text = text.Trim();

        if (text.IsEmpty)
        {
            return 0;
        }

        var index = 0;
        var isNegative = false;

        if (text[index] == '-')
        {
            isNegative = true;
            index++;
        }

        long integerPart = 0;
        long fractionalPart = 0;
        var fractionalDigits = 0;
        var roundDigit = -1;
        var isFractional = false;

        for (; index < text.Length; index++)
        {
            var ch = text[index];

            if (ch == '.')
            {
                if (isFractional)
                {
                    throw new FormatException($"價格格式錯誤：{text.ToString()}");
                }

                isFractional = true;
                continue;
            }

            if (ch < '0' || ch > '9')
            {
                throw new FormatException($"價格包含非數字字元：{text.ToString()}");
            }

            var digit = ch - '0';

            if (!isFractional)
            {
                integerPart = checked(integerPart * 10 + digit);
                continue;
            }

            if (fractionalDigits < 4)
            {
                fractionalPart = fractionalPart * 10 + digit;
                fractionalDigits++;
                continue;
            }

            if (roundDigit < 0)
            {
                roundDigit = digit;
            }
        }

        while (fractionalDigits < 4)
        {
            fractionalPart *= 10;
            fractionalDigits++;
        }

        var scaled = checked(integerPart * Scale + fractionalPart);

        if (roundDigit >= 5)
        {
            scaled++;
        }

        if (isNegative)
        {
            scaled = -scaled;
        }

        if (scaled > int.MaxValue || scaled < int.MinValue)
        {
            throw new OverflowException($"價格超出 int e4 範圍：{text.ToString()}");
        }

        return (int)scaled;
    }

    /// <summary>
    /// 將 e4 價格轉成人眼可讀價格。
    ///
    /// 這個方法只用於 Console / 報表輸出，
    /// 不應放在策略熱路徑內大量呼叫。
    /// </summary>
    public static string FormatPriceE4(int priceE4)
    {
        var isNegative = priceE4 < 0;
        var value = Math.Abs(priceE4);

        var integerPart = value / Scale;
        var fractionalPart = value % Scale;

        var text = fractionalPart == 0
            ? integerPart.ToString()
            : $"{integerPart}.{fractionalPart:0000}".TrimEnd('0');

        return isNegative ? "-" + text : text;
    }
}
