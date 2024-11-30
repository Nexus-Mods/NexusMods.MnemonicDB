using System;

namespace NexusMods.MnemonicDB.QueryEngine.Facts;

public interface IActiveTable
{
    
}

public interface IActiveTable<TFact> : IActiveTable
    where TFact : IFact
{
    public IObservable<(TFact Fact, int Delta)> Facts { get; }
}
