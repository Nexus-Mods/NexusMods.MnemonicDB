using System.Collections.Generic;
using System.Collections.Immutable;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.LogicEngine;

using LazyEnvStream = IEnumerable<IImmutableDictionary<LVar, object>>;

public interface IGoal
{
    public IPredicate Optimize(IPredicate input, ref ImmutableHashSet<LVar> preBound, LVar extract);
    public LazyEnvStream Run(IPredicate predicate, LazyEnvStream envs);
    public IObservableList<IImmutableDictionary<LVar, object>> Observe(IConnection conn);
}
