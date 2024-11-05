using System.Collections.Immutable;

namespace NexusMods.MnemonicDB.LogicEngine;

public interface IGoal
{
    public IPredicate Optimize(IPredicate input, ref ImmutableHashSet<LVar> preBound, LVar extract);
    
}
