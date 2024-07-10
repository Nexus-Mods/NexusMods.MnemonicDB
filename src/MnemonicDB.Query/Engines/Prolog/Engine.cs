using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData.Kernel;
using NexusMods.MnemonicDB.Abstractions;

namespace MnemonicDB.Query.Engines.Datalog.Prolog;

public class Engine
{
    private Dictionary<(Symbol Predicate, int Arity), List<IFactSource>> _sources = new();
    public Engine(IFactSource[] sources)
    {
        var tmp = new Dictionary<(Symbol Predicate, int Arity), List<IFactSource>>();
        
        void AddSource(int arity, IFactSource source)
        {
            if (!tmp.TryGetValue((source.Predicate, arity), out var list))
            {
                list = new List<IFactSource>();
                tmp.Add((source.Predicate, arity), list);
            }
            list.Add(source);
        }
        
        foreach (var source in sources)
        {
            if (source is IFactSource1 one) 
                AddSource(1, one);
            if (source is IFactSource2 two) 
                AddSource(2, two);
        }
        
        _sources = tmp;
    }

    public IEnumerable<TOut> Query<TIn, TOut>(IFact[] query, Term<TIn> inTerm, TIn binding, Term<TOut> outVar) 
        where TOut : notnull
    {
        var firstEnv = new HashmapEnvironment().Bind(inTerm, binding);
        return QueryInner(firstEnv, query, env =>
        {
            if (env.TryGet(outVar, out var value))
                return Optional.Some(value);
            return Optional.None<TOut>();
        });
    }
    
    public IEnumerable<TOut> QueryInner<TOut, TEnv>(TEnv initialEnv, IFact[] query, Func<TEnv, Optional<TOut>> selector)
    where TEnv : IEnvironment<TEnv> where TOut : notnull
    {
        IEnumerable<TEnv> envs = [initialEnv];
        
        foreach (var fact in query)
        {
            if (_sources.TryGetValue((fact.Predicate, fact.Arity), out var sources))
            {
                foreach (var source in sources)
                {
                    envs = source switch
                    {
                        IFactSource1 one => one.LazyDispatch(envs, fact),
                        IFactSource2 two => two.LazyDispatch(envs, fact),
                        _ => envs
                    };
                }
            }
        }

        foreach (var result in envs)
        {
            var selected = selector(result);
            if (!selected.HasValue)
                continue;
            yield return selected.Value;
        }
    }

    public IEnumerable<T> Query<T>(IFact[] query, Term<T> term) 
        where T : notnull
    {
        var firstEnv = new HashmapEnvironment();
        return QueryInner(firstEnv, query, env =>
        {
            if (env.TryGet(term, out var value))
                return Optional.Some(value);
            return Optional.None<T>();
        });
    }
}
