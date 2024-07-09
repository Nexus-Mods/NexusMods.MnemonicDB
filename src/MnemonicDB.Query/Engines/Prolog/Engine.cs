using System.Collections.Generic;
using System.Linq;
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

    public IEnumerable<T> Query<T>(IFact[] query, Term<T> term)
    {
        IEnumerable<HashmapEnvironment> envs = new List<HashmapEnvironment> { new() };
        
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

        return envs.Select(env =>
            {
                if (env.TryGet(term, out var value))
                    return (true, value);
                return (false, default!);
            })
            .Where(t => t.Item1)
            .Select(t => t.Item2!);
    }
}
