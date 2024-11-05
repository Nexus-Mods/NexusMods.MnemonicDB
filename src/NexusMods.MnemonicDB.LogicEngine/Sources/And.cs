using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions;
using LazyEnvStream = System.Collections.Generic.IEnumerable<System.Collections.Immutable.IImmutableDictionary<NexusMods.MnemonicDB.LogicEngine.LVar, object>>;

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

    public LazyEnvStream Run(IPredicate predicate, LazyEnvStream envs)
    {
        var p = (Predicate<And>)predicate;
        
        var acc = envs;
        foreach (var child in p.Args)
        {
            acc = ((IPredicate)child).Source.Run((IPredicate)child, acc);
        }

        return acc;
    }

    public IObservableList<IImmutableDictionary<LVar, object>> Observe(IConnection conn)
    {
        throw new NotImplementedException();
    }
}
