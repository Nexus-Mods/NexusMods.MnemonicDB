using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.LogicEngine.Sources;

namespace NexusMods.MnemonicDB.LogicEngine;

public static partial class QueryBuilder
{
    public static Predicate Datoms<TAttribute, THighLevel>(LVar<EntityId> eLvar, TAttribute attr, THighLevel value)
        where TAttribute : IWritableAttribute<THighLevel>, IReadableAttribute<THighLevel>
    {
        return new Datoms<TAttribute, THighLevel>
        {
            Arg1 = eLvar,
            Arg2 = attr,
            Arg3 = value
        };
    }
    
    public static Predicate Datoms<TAttribute, TValue>(LVar<EntityId> eLVar, TAttribute attr, LVar<TValue> value)
    where TAttribute : IWritableAttribute<TValue>, IReadableAttribute<TValue>
    {
        return new Datoms<TAttribute, TValue>
        {
            Arg1 = eLVar,
            Arg2 = attr,
            Arg3 = value
        };
    }
    
    public static Predicate And(params Predicate[] predicates)
    {
        return new And(predicates);
    }
}
