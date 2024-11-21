using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.QueryEngine.Predicates;

public record Datoms<TAttribute, TValue> : Predicate
where TAttribute : notnull
where TValue : notnull
{
    private readonly Term<EntityId> _e;
    private readonly Term<TAttribute> _a;
    private readonly Term<TValue> _v;

    public Datoms(Term<EntityId> e, Term<TAttribute> a, Term<TValue> v)
    {
        _e = e;
        _a = a;
        _v = v;
    }

    public override IEnumerable<(string Name, ITerm Term)> Terms
    {
        get
        {
            yield return (nameof(_e), _e);
            yield return (nameof(_a), _a);
            yield return (nameof(_v), _v);
        }
    }

    public override IEnumerable<ImmutableDictionary<LVar, object>> Apply(IEnumerable<ImmutableDictionary<LVar, object>> envStream)
    {
        throw new System.NotImplementedException();
    }
}
