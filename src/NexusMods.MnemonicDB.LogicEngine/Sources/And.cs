using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using EnvironmentStream = System.Collections.Generic.IEnumerable<System.Collections.Immutable.ImmutableDictionary<NexusMods.MnemonicDB.LogicEngine.LVar, object>>;

namespace NexusMods.MnemonicDB.LogicEngine.Sources;

public record And(Predicate[] Children) : Predicate
{
    public override Predicate Optimize(ref ImmutableHashSet<LVar> preBound, LVar extract)
    {
        var optimizedChildren = new List<Predicate>();
        foreach (var child in Children)
        {
            optimizedChildren.Add(child.Optimize(ref preBound, extract));
        }
        return new And(optimizedChildren.ToArray());
    }

    public override EnvironmentStream Run(IDb db, Predicate query, EnvironmentStream stream)
    {
        return Children.Aggregate(stream, (current, child) => child.Run(db, query, current));
    }
}
