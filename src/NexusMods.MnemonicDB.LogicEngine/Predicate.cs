using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.LogicEngine;

using EnvironmentStream = IEnumerable<ImmutableDictionary<LVar, object>>;

public abstract record Predicate
{
    public abstract Predicate Optimize(ref ImmutableHashSet<LVar> preBound, LVar extract);
    public abstract EnvironmentStream Run(IDb db, Predicate query, EnvironmentStream o);
}

public enum ArgType
{
    Constant,
    Variable,
    Unbound
}

public abstract record Predicate<T1, T2, T3> : Predicate
{
    public required Term<T1> Arg1 { get; init; }
    public required Term<T2> Arg2 { get; init; }
    public required Term<T3> Arg3 { get; init; }
    
    protected static ArgType Resolve<T>(in Term<T> term, ISet<LVar> boundVars)
    {
        if (term.IsValue) return ArgType.Constant;
        return boundVars.Contains(term.LVar) ? ArgType.Variable : ArgType.Unbound;
    }
    
    public override Predicate Optimize(ref ImmutableHashSet<LVar> preBound, LVar extract)
    {
        return this;
    }

    /// <summary>
    /// Resolve the arguments of this predicate, given the set of bound variables, returning a tuple
    /// of the resolved argument types.
    /// </summary>
    public (ArgType A1, ArgType A2, ArgType A3) Resolve(ISet<LVar> boundVars)
    {
        return (Resolve(Arg1, boundVars), Resolve(Arg2, boundVars), Resolve(Arg3, boundVars));
    }
}

