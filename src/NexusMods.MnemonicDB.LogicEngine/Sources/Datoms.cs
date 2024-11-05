using System;
using System.Collections.Immutable;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using EnvironmentStream = System.Collections.Generic.IEnumerable<System.Collections.Immutable.ImmutableDictionary<NexusMods.MnemonicDB.LogicEngine.LVar, object>>;
using LazyEnvStream = System.Collections.Generic.IEnumerable<System.Collections.Immutable.IImmutableDictionary<NexusMods.MnemonicDB.LogicEngine.LVar, object>>;

namespace NexusMods.MnemonicDB.LogicEngine.Sources;

public record Datoms<TAttribute, TValueType> : Predicate<EntityId, TAttribute, TValueType> 
    where TAttribute : IWritableAttribute<TValueType>, IReadableAttribute<TValueType>
{
    
    public override Predicate Optimize(ref ImmutableHashSet<LVar> preBound, LVar extract)
    {
        var pattern = Resolve(preBound);
        switch (pattern)
        {
            case (ArgType.Unbound, ArgType.Constant, ArgType.Constant):
                preBound = preBound.Add(Arg1.LVar);
                return new FindEByAV<TAttribute, TValueType>
                {
                    Arg1 = Arg1,
                    Arg2 = Arg2,
                    Arg3 = Arg3
                };
            case (ArgType.Variable, ArgType.Constant, ArgType.Unbound):
                preBound = preBound.Add(Arg3.LVar);
                return new FindVByAE<TAttribute, TValueType>
                {
                    Arg1 = Arg1,
                    Arg2 = Arg2,
                    Arg3 = Arg3
                };
            default:
                throw new NotSupportedException("Unsupported pattern : " + pattern);
        }
    }

    public override EnvironmentStream Run(IDb db, Predicate query, EnvironmentStream o)
    {
        throw new NotSupportedException("Must optimize before running");
    }
}

/// <summary>
/// Find an entityId by attribute and value
/// </summary>
public record FindEByAV<TAttribute, TValueType> : Predicate<EntityId, TAttribute, TValueType> 
    where TAttribute : IWritableAttribute<TValueType>, IReadableAttribute<TValueType>
{
    public override EnvironmentStream Run(IDb db, Predicate query, EnvironmentStream o)
    {
        var eLVar = Arg1.LVar;
        var vValue = Arg3.Value;
        var attr = Arg2.Value;
        foreach (var env in o)
        {
            foreach (var datom in db.Datoms(SliceDescriptor.Create(attr, vValue, db.AttributeCache)))
            {
                yield return env.Add(eLVar, datom.E);
            }
        }
    }
}

/// <summary>
/// Find a value by entity and attribute
/// </summary>
public record FindVByAE<TAttribute, TValueType> : Predicate<EntityId, TAttribute, TValueType> 
    where TAttribute : IWritableAttribute<TValueType>, IReadableAttribute<TValueType>
{
    public override EnvironmentStream Run(IDb db, Predicate query, EnvironmentStream o)
    {
        var eLVar = Arg1.LVar;
        var attr = Arg2.Value;
        var aId = db.AttributeCache.GetAttributeId(Arg2.Value.Id);
        var vLVar = Arg3.LVar;
        var resolver = db.Connection.AttributeResolver;
        foreach (var env in o)
        {
            var eVal = (EntityId)env[eLVar];
            foreach (var datom in db.Datoms(SliceDescriptor.Create(eVal, aId)))
            {
                yield return env.Add(vLVar, attr.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, resolver)!);  
            }
        }
    }
}
