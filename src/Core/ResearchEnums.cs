namespace BacktestEngine.Research.Contracts;
public enum ResearchTemporalRole : byte
{
    EventTimeFeature = 1,
    ForwardOutcome = 2,
    RetrospectiveLabel = 3
}
public enum ResearchTemporalMode : byte
{
    Causal = 1,
    DelayedConfirmation = 2,
    Retrospective = 3
}