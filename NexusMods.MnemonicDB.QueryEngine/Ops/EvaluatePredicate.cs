using System;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine.Facts;
using R3;

namespace NexusMods.MnemonicDB.QueryEngine.Ops;

/// <summary>
/// A operation that evaluates a predicate
/// </summary>
/// <param name="predicate"></param>
public class EvaluatePredicate<TFact> : IOp<TFact> 
    where TFact : IFact
{
    public required Predicate Predicate { get; init; }

    public IObservable<FactDelta<TFact>> Observe(IConnection conn)
    {
        return Predicate.Observe<TFact>(conn);
    }

    public ITable<TFact> Execute(IDb db)
    {
        return Predicate.Evaluate<TFact>(db);
    }
}
