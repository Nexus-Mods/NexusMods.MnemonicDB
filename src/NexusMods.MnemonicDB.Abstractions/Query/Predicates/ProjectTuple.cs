using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NexusMods.MnemonicDB.Abstractions.Query.Predicates;

public record ProjectTuple<T1, T2> : Predicate
{
    private readonly LVar<T1> _a;
    private readonly LVar<T2> _b;
    private readonly LVar<(T1, T2)> _out;
    
    public ProjectTuple(LVar<T1> a, LVar<T2> b, LVar<(T1, T2)> o)
    {
        _a = a;
        _b = b;
        _out = o;
    }

    public override IEnumerable<(string Name, ITerm Term)> Terms
    {
        get
        {
            yield return (nameof(_a), new Term<T1>(_a));
            yield return (nameof(_b), new Term<T2>(_b));
            yield return (nameof(_out), new Term<(T1, T2)>(_out));
        }
    }

    public override IEnumerable<ImmutableDictionary<LVar, object>> Apply(IEnumerable<ImmutableDictionary<LVar, object>> envStream)
    {
        foreach (var env in envStream)
        {
            if (!env.TryGetValue(_a, out var aVal) || !env.TryGetValue(_b, out var bVal))
            {
                continue;
            }
            yield return env.Add(_out, new ValueTuple<T1, T2>((T1)aVal, (T2)bVal));
        }
    }
}
