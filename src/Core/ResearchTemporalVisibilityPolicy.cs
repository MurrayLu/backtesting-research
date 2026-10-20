using BacktestEngine.Research.Contracts;
namespace BacktestEngine.Research.Application;
public sealed record ResearchTemporalViewOptions(bool CausalOnly=false, long? AsOfTimestampUs=null, bool IncludeDelayedConfirmation=true, bool IncludeRetrospectiveOutcomes=true, long? ChartWindowStartUs=null,long? ChartWindowEndUs=null);
public static class ResearchTemporalVisibilityPolicy
{
    public static bool IsVisible(ResearchTemporalEnvelope temporal, ResearchTemporalViewOptions view, long? resolvedAsOf = null)
    {
        var asOf = resolvedAsOf ?? view.AsOfTimestampUs;
        if (view.CausalOnly)
            return asOf.HasValue
                   && temporal.TemporalRole == ResearchTemporalRole.EventTimeFeature
                   && temporal.TemporalMode == ResearchTemporalMode.Causal
                   && temporal.AvailableAt <= asOf.Value;

        if (asOf.HasValue && temporal.AvailableAt > asOf.Value) return false;
        if (!view.IncludeDelayedConfirmation && temporal.TemporalMode == ResearchTemporalMode.DelayedConfirmation) return false;
        if (!view.IncludeRetrospectiveOutcomes
            && (temporal.TemporalRole != ResearchTemporalRole.EventTimeFeature
                || temporal.TemporalMode == ResearchTemporalMode.Retrospective)) return false;
        if (view.ChartWindowEndUs.HasValue && temporal.ObservedAt > view.ChartWindowEndUs.Value) return false;
        if (view.ChartWindowStartUs.HasValue
            && (temporal.HorizonEndAt ?? temporal.AvailableAt) < view.ChartWindowStartUs.Value) return false;
        return true;
    }

    public static long EffectiveZoneStart(long chartWindowStart, ResearchTemporalEnvelope temporal, long? payloadEffectiveFrom) =>
        Math.Max(chartWindowStart, Math.Max(temporal.AvailableAt, payloadEffectiveFrom ?? long.MinValue));
}