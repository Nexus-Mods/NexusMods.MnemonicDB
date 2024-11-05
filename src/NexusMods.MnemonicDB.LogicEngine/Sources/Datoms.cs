using System;
using System.Collections.Immutable;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using LazyEnvStream = System.Collections.Generic.IEnumerable<System.Collections.Immutable.IImmutableDictionary<NexusMods.MnemonicDB.LogicEngine.LVar, object>>;

namespace NexusMods.MnemonicDB.LogicEngine.Sources;

public class Datoms : IGoal
{
    public IPredicate Optimize(IPredicate input, ref ImmutableHashSet<LVar> preBound, LVar extract)
    {
        var p = (Predicate<Datoms>)input;
        var pattern = (p.GetArgType(0, preBound), p.GetArgType(1, preBound), p.GetArgType(2, preBound), p.GetArgType(3, preBound));
        
        switch (pattern)
        {
            case (ArgType.Variable, ArgType.Unbound, ArgType.Constant, ArgType.Constant):
                preBound = preBound.Add((LVar)p[0]);
                return p.WithName<FindEByAV>();
            case (ArgType.Variable, ArgType.Unbound, ArgType.Constant, ArgType.Unbound):
                preBound = preBound.Add((LVar)p[0]);
                return p.WithName<FindVByAE>();

            default:
                throw new NotSupportedException("Unsupported pattern : " + pattern);
        }
    }

    public LazyEnvStream Run(IPredicate predicate, LazyEnvStream envs)
    {
        throw new NotSupportedException("Must optimize before running");
    }

    public IObservableList<IImmutableDictionary<LVar, object>> Observe(IConnection conn)
    {
        throw new NotSupportedException("Must optimize before observing");
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

    public LazyEnvStream Run(IPredicate predicate, LazyEnvStream envs)
    {
        var p = (Predicate<FindEByAV>)predicate;
        var eLVar = (LVar)p[1];
        var aVal = (IAttribute)p[2];
        var vVal = p[3];
        
        foreach (var env in envs)
        {
            var db = (IDb)env[QueryBuilder.GlobalDb];
            var datoms = db.Datoms(aVal);
            foreach (var datom in datoms)
            {
                var resolved = datom.Resolved(db.Connection);
                if (resolved.ObjectValue.Equals(vVal))
                {
                    yield return env.Add(eLVar, resolved.E);
                }
            }
        }
    }

    public IObservableList<IImmutableDictionary<LVar, object>> Observe(IConnection conn)
    {
        throw new NotImplementedException();
    }

    public IObservableList<IImmutableDictionary<LVar, object>> Observe(IConnection conn, IPredicate predicate)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Find a value by entity and attribute
/// </summary>
public class FindVByAE : IGoal
{
    public IPredicate Optimize(IPredicate input, ref ImmutableHashSet<LVar> preBound, LVar extract)
    {
        return input;
    }

    public LazyEnvStream Run(IPredicate predicate, LazyEnvStream envs)
    {
        var p = (Predicate<FindVByAE>)predicate;
        var eLVar = (LVar)p[1];
        var aVal = (IAttribute)p[2];
        var vLVar = (LVar)p[3];

        foreach (var env in envs)
        {
            var e = (EntityId)env[eLVar];
            var db = (IDb)env[QueryBuilder.GlobalDb];
            var aId = db.AttributeCache.GetAttributeId(aVal.Id);
            var datoms = db.Datoms(SliceDescriptor.Create(e, aId));
            foreach (var datom in datoms)
            { 
                yield return env.Add(vLVar, datom.Resolved(db.Connection).ObjectValue);
            }
        }
    }

    public IObservableList<IImmutableDictionary<LVar, object>> Observe(IConnection conn)
    {
        throw new NotImplementedException();
    }
}
