namespace NexusMods.MnemonicDB.QueryEngine.Facts;

public readonly record struct FactDelta<TFact>(TFact Fact, int Delta)
    where TFact : IFact
{
}
