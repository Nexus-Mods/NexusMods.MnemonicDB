using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NexusMods.MnemonicDB.LogicEngine.Sources;

public class And : IGoal
{
    public IPredicate Optimize(IPredicate input, ref ImmutableHashSet<LVar> preBound, LVar extract)
    {
        var p = (Predicate<And>)input;
        var newChildren = new List<object>();
        foreach (var child in p.Args)
        {
            var childPredicate = child as IPredicate;
            if (childPredicate is null)
                throw new NotSupportedException("All children of an And must be predicates: " + child);
            
            newChildren.Add(childPredicate.Source.Optimize(childPredicate, ref preBound, extract));
        }
        
        return p.WithArgs(newChildren);
    }
}
