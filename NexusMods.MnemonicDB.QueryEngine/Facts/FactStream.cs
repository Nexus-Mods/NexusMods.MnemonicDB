using System;
using R3;

namespace NexusMods.MnemonicDB.QueryEngine.Facts;

public interface IFactStream
{
    
}

public class FactStream<TFact> : Observable<FactDelta<TFact>> 
    where TFact : IFact
{
    protected override IDisposable SubscribeCore(Observer<FactDelta<TFact>> observer)
    {
        throw new NotImplementedException();
    }
}
