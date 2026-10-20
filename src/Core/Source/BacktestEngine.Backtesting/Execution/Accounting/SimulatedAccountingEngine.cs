using BacktestEngine.Backtesting.Execution.Costs;
using BacktestEngine.Backtesting.Execution.Fills;
using BacktestEngine.Backtesting.Execution.Records;

namespace BacktestEngine.Backtesting.Execution.Accounting;

/// <summary>
/// 通用模擬會計引擎。
///
/// 職責：
/// - 根據單筆 fill 更新 signed position。
/// - 計算 position effect。
/// - 計算平均成本。
/// - 計算 realized gross pnl。
/// - 套用 fee/tax。
/// - 產生 SimulatedTradeRecord。
///
/// signed position lots：
/// - 多單為正數
/// - 空單為負數
/// - 空手為 0
/// </summary>
public sealed class SimulatedAccountingEngine
{
    private readonly FeeTaxSpec _feeTaxSpec;
    private readonly FeeTaxModel _feeTaxModel;

    /// <summary>
    /// 使用台股當沖預設費稅設定。
    /// </summary>
    public SimulatedAccountingEngine()
        : this(FeeTaxSpec.CreateDefaultTaiwanStockIntraday())
    {
    }

    /// <summary>
    /// 使用指定費稅設定。
    /// </summary>
    public SimulatedAccountingEngine(FeeTaxSpec feeTaxSpec)
    {
        _feeTaxSpec = feeTaxSpec ?? throw new ArgumentNullException(nameof(feeTaxSpec));
        _feeTaxSpec.Validate();

        _feeTaxModel = new FeeTaxModel();
    }

    /// <summary>
    /// 套用單筆 fill，產生新持倉狀態與交易紀錄。
    /// </summary>
    public SimulatedAccountingResult ApplyFill(
        SimulatedPositionState positionBefore,
        SimulatedFill fill)
    {
        ArgumentNullException.ThrowIfNull(positionBefore);
        ArgumentNullException.ThrowIfNull(fill);

        fill.Validate();
        ValidatePositionMatchesFill(positionBefore, fill);

        var beforeLots = positionBefore.PositionLots;
        var signedFillLots = GetSignedFillLots(fill);
        var afterLots = beforeLots + signedFillLots;

        var positionEffect = DeterminePositionEffect(
            beforeLots,
            afterLots);

        var averageEntryPriceE4Before =
            positionBefore.AverageEntryPriceE4;

        var closedLots = CalculateClosedLots(
            beforeLots,
            fill);

        var grossPnlTwd = CalculateGrossPnlTwd(
            beforeLots,
            averageEntryPriceE4Before,
            fill,
            closedLots);

        var positionEntryValueE4LotsAfter =
            CalculatePositionEntryValueE4LotsAfter(
                positionBefore,
                fill,
                signedFillLots,
                afterLots,
                closedLots);

        var feeTax = _feeTaxModel.CalculateForFill(
            _feeTaxSpec,
            fill.Side,
            fill.FillPriceE4,
            fill.Lots,
            fill.LotSizeShares);

        var netPnlTwd =
            grossPnlTwd -
            feeTax.CommissionTwd -
            feeTax.TaxTwd;

        var positionAfter = new SimulatedPositionState
        {
            Symbol = fill.Symbol,
            TradingDate = fill.TradingDate,
            PositionLots = afterLots,
            PositionEntryValueE4Lots = positionEntryValueE4LotsAfter,
            CurrentEntryCycleId = ResolveEntryCycleIdAfter(
                positionBefore,
                fill,
                signedFillLots,
                afterLots),
            FirstEntryFillTimestampUs = ResolveFirstEntryFillTimestampUsAfter(
                positionBefore,
                fill,
                afterLots,
                closedLots),

            RealizedGrossPnlTwd =
                positionBefore.RealizedGrossPnlTwd + grossPnlTwd,

            CommissionTwd =
                positionBefore.CommissionTwd + feeTax.CommissionTwd,

            TaxTwd =
                positionBefore.TaxTwd + feeTax.TaxTwd,

            RealizedNetPnlTwd =
                positionBefore.RealizedNetPnlTwd + netPnlTwd
        };

        var trade = new SimulatedTradeRecord
        {
            Sequence = fill.Sequence,
            StrategyId = fill.StrategyId,
            StrategyRunId = fill.StrategyRunId,
            Symbol = fill.Symbol,
            TradingDate = fill.TradingDate,

            Side = fill.Side,
            IntentKind = fill.IntentKind,
            PositionEffect = positionEffect,

            TriggerId = fill.TriggerId,
            TriggerCode = fill.TriggerCode,
            TriggerGroup = fill.TriggerGroup,
            TriggerName = fill.TriggerName,
            TriggerDetail = fill.TriggerDetail,
            EntryCycleId = fill.EntryCycleId,
            CorrelationId = fill.CorrelationId,

            TriggerTimestampUs = fill.TriggerTimestampUs,
            TriggerPriceE4 = fill.TriggerPriceE4,

            DueTimestampUs = fill.DueTimestampUs,
            SubmittedTimestampUs = fill.SubmittedTimestampUs,
            FillTimestampUs = fill.FillTimestampUs,

            DecisionPriceE4 = fill.DecisionPriceE4,
            RiskReferencePriceE4 = fill.RiskReferencePriceE4,
            RawFillPriceE4 = fill.RawFillPriceE4,
            FillPriceE4 = fill.FillPriceE4,
            RawSelectedBidPriceE4 = fill.RawSelectedBidPriceE4,
            RawSelectedAskPriceE4 = fill.RawSelectedAskPriceE4,
            RawSelectedLatestPriceE4 = fill.RawSelectedLatestPriceE4,
            QuoteConsistencyStatus = fill.QuoteConsistencyStatus,
            ConservativeFillApplied = fill.ConservativeFillApplied,
            ConservativeFillReason = fill.ConservativeFillReason,

            SlippageTicks = fill.SlippageTicks,
            SlippagePriceE4 = fill.SlippagePriceE4,

            Lots = fill.Lots,
            LotSizeShares = fill.LotSizeShares,

            PositionLotsBefore = positionBefore.PositionLots,
            PositionLotsAfter = positionAfter.PositionLots,
            AverageEntryPriceE4Before = averageEntryPriceE4Before,
            AverageEntryPriceE4After = positionAfter.AverageEntryPriceE4,

            GrossAmountTwd = feeTax.GrossAmountTwd,
            GrossPnlTwd = grossPnlTwd,
            CommissionTwd = feeTax.CommissionTwd,
            TaxTwd = feeTax.TaxTwd,
            NetPnlTwd = netPnlTwd,
            NetCashFlowTwd = feeTax.NetCashFlowTwd,

            Note = fill.Note
        };

        return new SimulatedAccountingResult
        {
            PositionBefore = positionBefore,
            PositionAfter = positionAfter,
            Trade = trade
        };
    }

    public SimulatedEntryCycleAccountingResult ApplyFills(
        IReadOnlyList<SimulatedFill> fills)
    {
        ArgumentNullException.ThrowIfNull(fills);

        var positions = new Dictionary<PositionKey, EntryCyclePosition>();
        var ledgerRecords = new List<SimulatedFillLedgerRecord>();
        var completedTrades = new List<CompletedTradeRecord>();
        var issues = new List<SimulatedAccountingIssue>();

        foreach (var fill in fills.OrderBy(x => x.FillTimestampUs).ThenBy(x => x.Sequence))
        {
            fill.Validate();

            var key = new PositionKey(
                fill.StrategyId,
                fill.Symbol,
                fill.TradingDate);

            if (!positions.TryGetValue(key, out var position))
            {
                if (IsExit(fill))
                {
                    issues.Add(CreateIssue(
                        SimulatedAccountingIssueReason.NoOpenPosition,
                        fill,
                        "Exit fill has no open entry-cycle position."));
                    continue;
                }

                position = EntryCyclePosition.Open(fill);
                positions.Add(key, position);
            }
            else
            {
                if (position.EntryCycleId != fill.EntryCycleId)
                {
                    issues.Add(CreateIssue(
                        IsEntry(fill)
                            ? SimulatedAccountingIssueReason.ActiveEntryCycleExists
                            : SimulatedAccountingIssueReason.EntryCycleMismatch,
                        fill,
                        "Fill entry-cycle id does not match the active position."));
                    continue;
                }
            }

            var fillResult = ApplyEntryCycleFill(position, fill);
            if (fillResult.Issue is not null)
            {
                issues.Add(fillResult.Issue);
                continue;
            }

            ledgerRecords.Add(fillResult.LedgerRecord!);

            if (position.IsFlat)
            {
                completedTrades.Add(position.ToCompletedTrade());
                positions.Remove(key);
            }
        }

        return new SimulatedEntryCycleAccountingResult
        {
            FillLedgerRecords = ledgerRecords,
            CompletedTradeRecords = completedTrades,
            OpenPositions = positions.Values
                .Select(x => x.ToPositionState())
                .ToArray(),
            AccountingIssues = issues
        };
    }

    private EntryCycleFillApplyResult ApplyEntryCycleFill(
        EntryCyclePosition position,
        SimulatedFill fill)
    {
        var beforeLots = position.PositionLots;
        var signedFillLots = GetSignedFillLots(fill);
        var afterLots = beforeLots + signedFillLots;

        var positionEffect = DeterminePositionEffect(
            beforeLots,
            afterLots);

        if (positionEffect == SimulatedPositionEffect.Flip)
        {
            return EntryCycleFillApplyResult.Rejected(CreateIssue(
                SimulatedAccountingIssueReason.UnsupportedFlip,
                fill,
                "Entry-cycle accounting does not support position flips."));
        }

        if (beforeLots != 0
            && Math.Abs(afterLots) > Math.Abs(beforeLots)
            && !IsSameDirection(beforeLots, signedFillLots))
        {
            return EntryCycleFillApplyResult.Rejected(CreateIssue(
                SimulatedAccountingIssueReason.UnsupportedConcurrentEntryCycle,
                fill,
                "Fill would increase a position in the opposite direction."));
        }

        var closedLots = CalculateClosedLots(
            beforeLots,
            fill);

        if (IsExit(fill) && closedLots == 0)
        {
            return EntryCycleFillApplyResult.Rejected(CreateIssue(
                SimulatedAccountingIssueReason.NoOpenPosition,
                fill,
                "Exit fill does not reduce the open position."));
        }

        if (IsExit(fill) && fill.Lots > Math.Abs(beforeLots))
        {
            return EntryCycleFillApplyResult.Rejected(CreateIssue(
                SimulatedAccountingIssueReason.CloseLotsExceedOpenLots,
                fill,
                "Exit fill lots exceed the open position lots."));
        }

        var averageEntryPriceE4Before = position.AverageEntryPriceE4;
        var grossPnlTwd = CalculateGrossPnlTwd(
            beforeLots,
            averageEntryPriceE4Before,
            fill,
            closedLots);

        var positionEntryValueE4LotsAfter =
            CalculatePositionEntryValueE4LotsAfter(
                position.ToPositionState(),
                fill,
                signedFillLots,
                afterLots,
                closedLots);

        var feeTax = _feeTaxModel.CalculateForFill(
            _feeTaxSpec,
            fill.Side,
            fill.FillPriceE4,
            fill.Lots,
            fill.LotSizeShares);

        position.ApplyFill(
            fill,
            afterLots,
            positionEntryValueE4LotsAfter,
            grossPnlTwd,
            feeTax.CommissionTwd,
            feeTax.TaxTwd,
            feeTax.GrossAmountTwd,
            closedLots);

        var ledgerRecord = new SimulatedFillLedgerRecord
        {
            StrategyId = fill.StrategyId,
            StrategyRunId = fill.StrategyRunId,
            Symbol = fill.Symbol,
            TradingDate = fill.TradingDate,
            EntryCycleId = fill.EntryCycleId,
            CorrelationId = fill.CorrelationId,
            FillId = fill.Sequence,
            Side = fill.Side,
            IntentKind = fill.IntentKind,
            Lots = fill.Lots,
            LotSizeShares = fill.LotSizeShares,
            FillPriceE4 = fill.FillPriceE4,
            FillTimestampUs = fill.FillTimestampUs,
            GrossNotionalTwd = feeTax.GrossAmountTwd,
            PositionEffect = positionEffect,
            PositionLotsAfter = position.PositionLots,
            AverageEntryPriceE4After = position.AverageEntryPriceE4,
            RealizedGrossPnlTwd = grossPnlTwd,
            CommissionTwd = feeTax.CommissionTwd,
            TransactionTaxTwd = feeTax.TaxTwd,
            NetRealizedPnlTwd = grossPnlTwd - feeTax.CommissionTwd - feeTax.TaxTwd,
            TriggerCode = fill.TriggerCode,
            TriggerDetailCode = fill.TriggerDetail,
            TriggerEvidence = fill.TriggerEvidence,
            EntryTriggerEvidence = IsExit(fill)
                ? position.EntryTriggerEvidence
                : fill.TriggerEvidence
        };

        return EntryCycleFillApplyResult.Accepted(ledgerRecord);
    }

    private static bool IsEntry(
        SimulatedFill fill)
    {
        return fill.IntentKind == SimulatedTradeIntentKind.Entry;
    }

    private static bool IsExit(
        SimulatedFill fill)
    {
        return fill.IntentKind == SimulatedTradeIntentKind.Exit;
    }

    private static SimulatedAccountingIssue CreateIssue(
        SimulatedAccountingIssueReason reason,
        SimulatedFill fill,
        string message)
    {
        return new SimulatedAccountingIssue
        {
            Reason = reason,
            Message = message,
            EntryCycleId = fill.EntryCycleId
        };
    }

    private static void ValidatePositionMatchesFill(
        SimulatedPositionState positionBefore,
        SimulatedFill fill)
    {
        if (!string.Equals(
                positionBefore.Symbol,
                fill.Symbol,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Position symbol 與 fill symbol 不一致。Position={positionBefore.Symbol}, Fill={fill.Symbol}");
        }

        if (positionBefore.TradingDate != fill.TradingDate)
        {
            throw new InvalidOperationException(
                $"Position trading date 與 fill trading date 不一致。Position={positionBefore.TradingDate:yyyy-MM-dd}, Fill={fill.TradingDate:yyyy-MM-dd}");
        }
    }

    private static int GetSignedFillLots(SimulatedFill fill)
    {
        return fill.Side switch
        {
            SimulatedTradeSide.Buy => fill.Lots,
            SimulatedTradeSide.Sell => -fill.Lots,
            _ => throw new ArgumentOutOfRangeException(nameof(fill.Side))
        };
    }

    private static SimulatedPositionEffect DeterminePositionEffect(
        int beforeLots,
        int afterLots)
    {
        if (beforeLots == 0 && afterLots != 0)
            return SimulatedPositionEffect.Open;

        if (beforeLots != 0 && afterLots == 0)
            return SimulatedPositionEffect.Close;

        if (beforeLots != 0
            && afterLots != 0
            && Math.Sign(beforeLots) != Math.Sign(afterLots))
        {
            return SimulatedPositionEffect.Flip;
        }

        if (Math.Abs(afterLots) > Math.Abs(beforeLots))
            return SimulatedPositionEffect.Increase;

        if (Math.Abs(afterLots) < Math.Abs(beforeLots))
            return SimulatedPositionEffect.Reduce;

        return SimulatedPositionEffect.Unknown;
    }

    private static int CalculateClosedLots(
        int beforeLots,
        SimulatedFill fill)
    {
        if (beforeLots > 0 && fill.Side == SimulatedTradeSide.Sell)
            return Math.Min(fill.Lots, beforeLots);

        if (beforeLots < 0 && fill.Side == SimulatedTradeSide.Buy)
            return Math.Min(fill.Lots, Math.Abs(beforeLots));

        return 0;
    }

    private static long CalculateGrossPnlTwd(
        int beforeLots,
        int averageEntryPriceE4Before,
        SimulatedFill fill,
        int closedLots)
    {
        if (closedLots <= 0)
            return 0;

        long priceDiffE4;

        if (beforeLots > 0 && fill.Side == SimulatedTradeSide.Sell)
        {
            // 平多：sell price - average entry price
            priceDiffE4 =
                (long)fill.FillPriceE4 -
                averageEntryPriceE4Before;
        }
        else if (beforeLots < 0 && fill.Side == SimulatedTradeSide.Buy)
        {
            // 回補空：average entry price - buy price
            priceDiffE4 =
                (long)averageEntryPriceE4Before -
                fill.FillPriceE4;
        }
        else
        {
            return 0;
        }

        return priceDiffE4 *
               closedLots *
               fill.LotSizeShares /
               10_000L;
    }

    private static long CalculatePositionEntryValueE4LotsAfter(
        SimulatedPositionState positionBefore,
        SimulatedFill fill,
        int signedFillLots,
        int afterLots,
        int closedLots)
    {
        var beforeLots = positionBefore.PositionLots;

        if (afterLots == 0)
            return 0;

        if (beforeLots == 0)
        {
            return (long)fill.FillPriceE4 *
                   Math.Abs(afterLots);
        }

        if (IsSameDirection(beforeLots, signedFillLots))
        {
            return positionBefore.PositionEntryValueE4Lots +
                   (long)fill.FillPriceE4 * fill.Lots;
        }

        if (Math.Sign(beforeLots) != Math.Sign(afterLots))
        {
            return (long)fill.FillPriceE4 *
                   Math.Abs(afterLots);
        }

        if (closedLots > 0)
        {
            var absBeforeLots = Math.Abs(beforeLots);

            var closedEntryValueE4Lots =
                positionBefore.PositionEntryValueE4Lots *
                closedLots /
                absBeforeLots;

            return positionBefore.PositionEntryValueE4Lots -
                   closedEntryValueE4Lots;
        }

        return positionBefore.PositionEntryValueE4Lots;
    }

    private static bool IsSameDirection(
        int beforeLots,
        int signedFillLots)
    {
        if (beforeLots == 0 || signedFillLots == 0)
            return false;

        return Math.Sign(beforeLots) == Math.Sign(signedFillLots);
    }

    private static Guid ResolveEntryCycleIdAfter(
        SimulatedPositionState positionBefore,
        SimulatedFill fill,
        int signedFillLots,
        int afterLots)
    {
        if (afterLots == 0)
            return Guid.Empty;

        if (positionBefore.PositionLots == 0)
            return fill.EntryCycleId;

        if (Math.Sign(positionBefore.PositionLots) != Math.Sign(afterLots))
            return fill.EntryCycleId;

        if (IsSameDirection(positionBefore.PositionLots, signedFillLots)
            && positionBefore.CurrentEntryCycleId == Guid.Empty)
        {
            return fill.EntryCycleId;
        }

        return positionBefore.CurrentEntryCycleId;
    }

    private static long ResolveFirstEntryFillTimestampUsAfter(
        SimulatedPositionState positionBefore,
        SimulatedFill fill,
        int afterLots,
        int closedLots)
    {
        if (afterLots == 0)
            return 0;

        if (positionBefore.FirstEntryFillTimestampUs > 0)
            return positionBefore.FirstEntryFillTimestampUs;

        return (IsEntry(fill) || closedLots == 0)
            ? fill.FillTimestampUs
            : 0;
    }

    private readonly record struct PositionKey(
        string StrategyId,
        string Symbol,
        DateOnly TradingDate);

    private sealed class EntryCycleFillApplyResult
    {
        private EntryCycleFillApplyResult(
            SimulatedFillLedgerRecord? ledgerRecord,
            SimulatedAccountingIssue? issue)
        {
            LedgerRecord = ledgerRecord;
            Issue = issue;
        }

        public SimulatedFillLedgerRecord? LedgerRecord { get; }

        public SimulatedAccountingIssue? Issue { get; }

        public static EntryCycleFillApplyResult Accepted(
            SimulatedFillLedgerRecord ledgerRecord)
        {
            return new EntryCycleFillApplyResult(
                ledgerRecord,
                issue: null);
        }

        public static EntryCycleFillApplyResult Rejected(
            SimulatedAccountingIssue issue)
        {
            return new EntryCycleFillApplyResult(
                ledgerRecord: null,
                issue);
        }
    }

    private sealed class EntryCyclePosition
    {
        public required string StrategyId { get; init; }

        public string StrategyRunId { get; init; } = "";

        public required string Symbol { get; init; }

        public DateOnly TradingDate { get; init; }

        public Guid EntryCycleId { get; init; }

        public required string Direction { get; init; }

        public SimulatedTradeSide EntrySide { get; init; }

        public SimulatedTradeSide ExitSide { get; private set; }

        public int PositionLots { get; private set; }

        public long PositionEntryValueE4Lots { get; private set; }

        public int AverageEntryPriceE4 =>
            PositionLots == 0
                ? 0
                : (int)(PositionEntryValueE4Lots / Math.Abs(PositionLots));

        public bool IsFlat => PositionLots == 0;

        public long EntryStartedAtTimestampUs { get; private set; }

        public long ExitCompletedAtTimestampUs { get; private set; }

        public long FirstEntryFillTimestampUs { get; private set; }

        public long LastExitFillTimestampUs { get; private set; }

        public int TotalEntryLots { get; private set; }

        public int TotalExitLots { get; private set; }

        public long TotalEntryShares { get; private set; }

        public long TotalExitShares { get; private set; }

        public long EntryNotionalTwd { get; private set; }

        public long ExitNotionalTwd { get; private set; }

        public long RealizedGrossPnlTwd { get; private set; }

        public long TotalCommissionTwd { get; private set; }

        public long TotalTransactionTaxTwd { get; private set; }

        public string EntryTriggerCode { get; private set; } = "";

        public string EntryTriggerDetailCode { get; private set; } = "";

        public IReadOnlyList<BacktestEngine.Contracts.Signals.TriggerEvidenceItem> EntryTriggerEvidence { get; private set; } =
            Array.Empty<BacktestEngine.Contracts.Signals.TriggerEvidenceItem>();

        public string ExitTriggerCode { get; private set; } = "";

        public string ExitTriggerDetailCode { get; private set; } = "";

        public Guid EntryCorrelationId { get; private set; }

        public Guid ExitCorrelationId { get; private set; }

        public int EntryFillCount { get; private set; }

        public int ExitFillCount { get; private set; }

        public static EntryCyclePosition Open(
            SimulatedFill fill)
        {
            var signedFillLots = GetSignedFillLots(fill);

            return new EntryCyclePosition
            {
                StrategyId = fill.StrategyId,
                StrategyRunId = fill.StrategyRunId,
                Symbol = fill.Symbol,
                TradingDate = fill.TradingDate,
                EntryCycleId = fill.EntryCycleId,
                Direction = signedFillLots > 0 ? "Long" : "Short",
                EntrySide = fill.Side
            };
        }

        public void ApplyFill(
            SimulatedFill fill,
            int positionLotsAfter,
            long positionEntryValueE4LotsAfter,
            long grossPnlTwd,
            long commissionTwd,
            long taxTwd,
            long grossNotionalTwd,
            int closedLots)
        {
            if (IsEntry(fill) || closedLots == 0)
            {
                if (EntryFillCount == 0)
                {
                    EntryStartedAtTimestampUs = fill.TriggerTimestampUs;
                    FirstEntryFillTimestampUs = fill.FillTimestampUs;
                    EntryTriggerCode = fill.TriggerCode;
                    EntryTriggerDetailCode = fill.TriggerDetail;
                    EntryTriggerEvidence = fill.TriggerEvidence;
                    EntryCorrelationId = fill.CorrelationId;
                }

                TotalEntryLots += fill.Lots;
                TotalEntryShares += fill.Shares;
                EntryNotionalTwd += grossNotionalTwd;
                EntryFillCount++;
            }
            else
            {
                ExitSide = fill.Side;
                ExitCompletedAtTimestampUs = fill.FillTimestampUs;
                LastExitFillTimestampUs = fill.FillTimestampUs;
                ExitTriggerCode = fill.TriggerCode;
                ExitTriggerDetailCode = fill.TriggerDetail;
                ExitCorrelationId = fill.CorrelationId;
                TotalExitLots += fill.Lots;
                TotalExitShares += fill.Shares;
                ExitNotionalTwd += grossNotionalTwd;
                ExitFillCount++;
            }

            PositionLots = positionLotsAfter;
            PositionEntryValueE4Lots = positionEntryValueE4LotsAfter;
            RealizedGrossPnlTwd += grossPnlTwd;
            TotalCommissionTwd += commissionTwd;
            TotalTransactionTaxTwd += taxTwd;
        }

        public SimulatedPositionState ToPositionState()
        {
            return new SimulatedPositionState
            {
                Symbol = Symbol,
                TradingDate = TradingDate,
                PositionLots = PositionLots,
                PositionEntryValueE4Lots = PositionEntryValueE4Lots,
                CurrentEntryCycleId = EntryCycleId,
                FirstEntryFillTimestampUs = FirstEntryFillTimestampUs,
                RealizedGrossPnlTwd = RealizedGrossPnlTwd,
                CommissionTwd = TotalCommissionTwd,
                TaxTwd = TotalTransactionTaxTwd,
                RealizedNetPnlTwd =
                    RealizedGrossPnlTwd -
                    TotalCommissionTwd -
                    TotalTransactionTaxTwd
            };
        }

        public CompletedTradeRecord ToCompletedTrade()
        {
            var averageExitPriceE4 = TotalExitLots == 0
                ? 0
                : (int)(ExitNotionalTwd * 10_000L / TotalExitShares);

            return new CompletedTradeRecord
            {
                StrategyId = StrategyId,
                StrategyRunId = StrategyRunId,
                Symbol = Symbol,
                TradingDate = TradingDate,
                EntryCycleId = EntryCycleId,
                Direction = Direction,
                EntryStartedAtTimestampUs = EntryStartedAtTimestampUs,
                ExitCompletedAtTimestampUs = ExitCompletedAtTimestampUs,
                FirstEntryFillTimestampUs = FirstEntryFillTimestampUs,
                LastExitFillTimestampUs = LastExitFillTimestampUs,
                EntrySide = EntrySide,
                ExitSide = ExitSide,
                TotalEntryLots = TotalEntryLots,
                TotalExitLots = TotalExitLots,
                TotalEntryShares = TotalEntryShares,
                TotalExitShares = TotalExitShares,
                AverageEntryPriceE4 = TotalEntryLots == 0
                    ? 0
                    : (int)(EntryNotionalTwd * 10_000L / TotalEntryShares),
                AverageExitPriceE4 = averageExitPriceE4,
                EntryNotionalTwd = EntryNotionalTwd,
                ExitNotionalTwd = ExitNotionalTwd,
                RealizedGrossPnlTwd = RealizedGrossPnlTwd,
                TotalCommissionTwd = TotalCommissionTwd,
                TotalTransactionTaxTwd = TotalTransactionTaxTwd,
                RealizedNetPnlTwd =
                    RealizedGrossPnlTwd -
                    TotalCommissionTwd -
                    TotalTransactionTaxTwd,
                EntryTriggerCode = EntryTriggerCode,
                EntryTriggerDetailCode = EntryTriggerDetailCode,
                ExitTriggerCode = ExitTriggerCode,
                ExitTriggerDetailCode = ExitTriggerDetailCode,
                EntryCorrelationId = EntryCorrelationId,
                ExitCorrelationId = ExitCorrelationId,
                EntryFillCount = EntryFillCount,
                ExitFillCount = ExitFillCount,
                EntryTriggerEvidence = EntryTriggerEvidence
            };
        }
    }
}
