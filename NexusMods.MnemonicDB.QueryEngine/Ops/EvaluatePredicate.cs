using System;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryEngine.Facts;

namespace NexusMods.MnemonicDB.QueryEngine.Ops;

/// <summary>
/// A operation that evaluates a predicate
/// </summary>
/// <param name="predicate"></param>
public class EvaluatePredicate : IOp
{
    public required Predicate Predicate { get; init; }

    public ITable Execute(IDb db)
    {
        return Predicate.Evaluate(db);
    }

    public LVar[] LVars => Predicate.LVars.ToArray();
    
    public Type FactType => Predicate.FactType;
}
