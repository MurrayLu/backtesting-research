namespace BacktestEngine.Contracts.Common;

[Flags]
public enum ProbeTickFlags : byte
{
    None = 0,
    Trial = 1,
    Simulated = 2
}
