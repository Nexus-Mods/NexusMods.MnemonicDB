using System;
using System.Collections.Immutable;

namespace NexusMods.MnemonicDB.LogicEngine.Sources;

public class Datoms : IGoal
{
    public IPredicate Optimize(IPredicate input, ref ImmutableHashSet<LVar> preBound, LVar extract)
    {
        var p = (Predicate<Datoms>)input;
        var pattern = (p.IsBound(0, preBound), p.IsBound(1, preBound), p.IsBound(2, preBound), p.IsBound(3, preBound));
        
        switch (pattern)
        {
            case (true, false, true, true):
                preBound = preBound.Add((LVar)p[1]);
                return p.WithName<FindEByAV>();
            case (true, true, true, false):
                preBound = preBound.Add((LVar)p[3]);
                return p.WithName<FindVByEA>();
            default:
                throw new NotSupportedException("Unsupported pattern : " + pattern);
        }
    }
}

/// <summary>
/// Find a entity by attribute and value
/// </summary>
public class FindEByAV : IGoal
{
    public IPredicate Optimize(IPredicate input, ref ImmutableHashSet<LVar> preBound, LVar extract)
    {
        return input;
    }
}

/// <summary>
/// Find a value by entity and attribute
/// </summary>
public class FindVByEA : IGoal
{
    public IPredicate Optimize(IPredicate input, ref ImmutableHashSet<LVar> preBound, LVar extract)
    {
        return input;
    }
}
