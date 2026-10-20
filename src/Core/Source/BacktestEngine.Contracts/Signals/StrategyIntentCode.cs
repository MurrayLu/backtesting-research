namespace BacktestEngine.Contracts.Signals;

/// <summary>
/// 策略 trigger identity。
///
/// 這是策略提供給 execution / accounting / reporting 的原因資訊。
///
/// 設計原則：
/// - TriggerId 給程式統計、排序、比對使用。
/// - TriggerCode 給 CSV / JSON / AI Review / 人工檢查使用。
/// - TriggerGroup 給報表聚合使用。
/// - TriggerName / TriggerDetail 給人類閱讀使用。
///
/// 這個類別不應該寫死 fade / trend 的所有原因；
/// 各策略自己定義自己的 trigger id / code。
/// </summary>
public sealed class StrategyIntentCode
{
    /// <summary>
    /// 策略內部 trigger 數字 ID。
    /// </summary>
    public int TriggerId { get; init; }

    /// <summary>
    /// 穩定文字代號。
    ///
    /// 範例：
    /// - ENTRY_SHORT_FADE
    /// - EXIT_SHORT_TIME
    /// - ENTRY_LONG_GENERAL_TREND
    /// - EXIT_LONG_HARD_STOP
    /// </summary>
    public required string TriggerCode { get; init; }

    /// <summary>
    /// trigger 分組。
    ///
    /// 範例：
    /// - FADE
    /// - TIME_EXIT
    /// - STOP
    /// - GENERAL_TREND
    /// </summary>
    public string TriggerGroup { get; init; } = "";

    /// <summary>
    /// 人類可讀名稱。
    /// </summary>
    public string TriggerName { get; init; } = "";

    /// <summary>
    /// 完整原因說明。
    /// </summary>
    public string TriggerDetail { get; init; } = "";

    /// <summary>
    /// 建立 trigger identity。
    /// </summary>
    public static StrategyIntentCode Create(
        int triggerId,
        string triggerCode,
        string triggerGroup = "",
        string triggerName = "",
        string triggerDetail = "")
    {
        var code = new StrategyIntentCode
        {
            TriggerId = triggerId,
            TriggerCode = triggerCode,
            TriggerGroup = triggerGroup,
            TriggerName = triggerName,
            TriggerDetail = triggerDetail
        };

        code.Validate();

        return code;
    }

    /// <summary>
    /// 驗證 trigger identity。
    /// </summary>
    public void Validate()
    {
        if (TriggerId <= 0)
            throw new InvalidOperationException("TriggerId 必須大於 0。");

        if (string.IsNullOrWhiteSpace(TriggerCode))
            throw new InvalidOperationException("TriggerCode 不可為空。");
    }
}
