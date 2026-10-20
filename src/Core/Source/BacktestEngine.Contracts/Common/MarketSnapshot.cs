namespace BacktestEngine.Contracts.Common;

/// <summary>
/// Replay 使用的市場狀態快照。
///
/// 這不是 raw tick。
/// 同一 timestamp 的 raw tick burst 會被合併成一筆 MarketSnapshot，
/// 讓策略 consume 的語義接近實盤 LatestTickSlot 的最新市場狀態。
/// </summary>
public readonly record struct MarketSnapshot
{
    /// <summary>
    /// 數字化商品 ID。
    /// 現階段可直接使用股票代碼轉換值；未接 symbol registry 時可為 0。
    /// </summary>
    public int SymbolId { get; init; }

    /// <summary>
    /// 市場資料來源。
    /// </summary>
    public ProbeTickSource Source { get; init; }

    /// <summary>
    /// Snapshot 時間，單位 microseconds。
    /// </summary>
    public long TimestampUs { get; init; }

    /// <summary>
    /// 同 timestamp burst 中最後一筆 raw tick 的來源序號。
    /// </summary>
    public int SourceSequence { get; init; }

    /// <summary>
    /// 最新成交價 e4。
    /// 同 timestamp burst 中取最後一筆 raw tick 的成交價。
    /// </summary>
    public int LatestPriceE4 { get; init; }

    /// <summary>
    /// Snapshot 區間最高價 e4。
    /// </summary>
    public int HighPriceE4 { get; init; }

    /// <summary>
    /// Snapshot 區間最低價 e4。
    /// </summary>
    public int LowPriceE4 { get; init; }

    /// <summary>
    /// 買價 e4。Tick CSV 尚未提供 bid/ask 時可為 0。
    /// </summary>
    public int BidPriceE4 { get; init; }

    /// <summary>
    /// 賣價 e4。Tick CSV 尚未提供 bid/ask 時可為 0。
    /// </summary>
    public int AskPriceE4 { get; init; }

    public int? CurrentVwapE4 { get; init; }

    public long VwapVolumeLots { get; init; }

    /// <summary>
    /// Snapshot 區間量。
    /// </summary>
    public int SizeLots { get; init; }

    /// <summary>
    /// 同 timestamp burst 中最後一筆 raw tick 的累計量。
    /// </summary>
    public int TotalVolumeLots { get; init; }

    /// <summary>
    /// Tick flags。
    /// </summary>
    public ProbeTickFlags Flags { get; init; }
}
