using System.Collections.Generic;

namespace NexusMods.Query.Abstractions.Engines;

public enum ResolveType
{
    Constant,
    LVar,
    Unbound
}


public class Context
{
    public Dictionary<ILVar, int> LVars { get; init; } = new();
    public HashSet<ILVar> Bound { get; init; } = new();
    
    public (ResolveType, int) Resolve<T>(Term<T> term)
        where T : notnull
    {
        if (term.Constant.HasValue)
            return (ResolveType.Constant, -1);
        if (term.Variable.HasValue && Bound.Contains(term.Variable.Value))
            return (ResolveType.LVar, LVars[term.Variable.Value]);
        Bound.Add(term.Variable.Value);
        return (ResolveType.Unbound, LVars[term.Variable.Value]);
    }
}
