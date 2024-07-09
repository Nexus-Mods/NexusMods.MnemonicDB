using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;

namespace MnemonicDB.Query.Engines.Datalog;

public interface IFactSource
{
    public Symbol Predicate { get; }
}

public interface IFactSource1 : IFactSource
{
    IEnumerable<TEnv> LazyDispatch<TEnv>(IEnumerable<TEnv> envs, IFact fact)
        where TEnv : IEnvironment<TEnv>;
}

public interface IFactSource2 : IFactSource
{
    IEnumerable<TEnv> LazyDispatch<TEnv>(IEnumerable<TEnv> envs, IFact fact)
        where TEnv : IEnvironment<TEnv>;
}

public interface IFactSource<TA> : IFactSource1
{
    public IEnumerable<TEnv> Lazy<TEnv>(IEnumerable<TEnv> envs, Fact<TA> fact) 
        where TEnv : IEnvironment<TEnv>;

    IEnumerable<TEnv> LazyDispatch<TEnv>(IEnumerable<TEnv> envs, IFact fact)
        where TEnv : IEnvironment<TEnv>
    {
        if (fact is Fact<TA> f)
            return Lazy(envs, f);
        return envs;
    }
}

public abstract class IFactSource<TA, TB> : IFactSource2
{
    public abstract Symbol Predicate { get; }
    public abstract IEnumerable<TEnv> Lazy<TEnv>(IEnumerable<TEnv> envs, Fact<TA, TB> fact) 
        where TEnv : IEnvironment<TEnv>;

    public IEnumerable<TEnv> LazyDispatch<TEnv>(IEnumerable<TEnv> envs, IFact fact)
        where TEnv : IEnvironment<TEnv>
    {
        if (fact is Fact<TA, TB> f)
            return Lazy(envs, f);
        return envs;
    }
}
