using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine.Facts;
using R3;

namespace NexusMods.MnemonicDB.QueryEngine.Ops;

public interface IOp
{
}

public interface IOp<TFact> : IOp
    where TFact : IFact
{
    public ITable<TFact> Execute(IDb db);
    
    public IObservable<FactDelta<TFact>> Observe(IConnection conn);
}
