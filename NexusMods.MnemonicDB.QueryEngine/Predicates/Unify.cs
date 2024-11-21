using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.QueryEngine.Tables;

namespace NexusMods.MnemonicDB.QueryEngine.Predicates;

public record Unify<T> : Predicate
 where T : IEquatable<T>
{
    public Unify(LVar<T> a, LVar<T> b)
    {
        A = a;
        B = b;
    }

    public LVar<T> B { get; init; }

    public LVar<T> A { get; init; }
    public override IEnumerable<(string Name, ITerm Term)> Terms
    {
        get
        {
            yield return (nameof(A), new Term<T>(A));
            yield return (nameof(B), new Term<T>(B));
        }
    }

    public override IEnumerable<ImmutableDictionary<LVar, object>> Apply(IEnumerable<ImmutableDictionary<LVar, object>> envStream)
    {
        throw new System.NotImplementedException();
    }

    [UsedImplicitly]
    private ITable Run_II(ITable input, int aIdx, int bIdx)
    {
        var e = Joiner!.GetEnumerator(input);
        while (e.MoveNext())
        {
            var a = e.Get<T>(aIdx);
            var b = e.Get<T>(bIdx);
            if (a.Equals(b))
            {
                e.FinishRow();
            }
        }
        return e.FinishTable();
    }
    
}
