using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;

namespace NexusMods.Query.Abstractions.Facts;

public record struct Datom<THighLevel, TLowLevel> : IFact<EntityId, Attribute<THighLevel, TLowLevel>, THighLevel>
{
    
}

public static class DatomExtensions
{
    public static QueryBuilder Datom<THighLevel, TLowLevel>(this QueryBuilder queryBuilder, Term<EntityId> eid, Attribute<THighLevel, TLowLevel> attr, Term<THighLevel> val) 
        where TLowLevel : notnull 
        where THighLevel : notnull
    {
        return queryBuilder.Where<Datom<THighLevel, TLowLevel>, EntityId, Attribute<THighLevel, TLowLevel>, THighLevel>(eid, attr, val);
    }
    
    public static QueryBuilder Datom<THighLevel, TLowLevel>(this QueryBuilder queryBuilder, out LVar<EntityId> eid, Attribute<THighLevel, TLowLevel> attr, Term<THighLevel> val) 
        where TLowLevel : notnull 
        where THighLevel : notnull
    {
        eid = new LVar<EntityId>();
        return queryBuilder.Where<Datom<THighLevel, TLowLevel>, EntityId, Attribute<THighLevel, TLowLevel>, THighLevel>(eid, attr, val);
    }
    
    public static QueryBuilder Datom<THighLevel, TLowLevel>(this QueryBuilder queryBuilder, out LVar<EntityId> eid, Attribute<THighLevel, TLowLevel> attr, out LVar<THighLevel> val) 
        where TLowLevel : notnull 
        where THighLevel : notnull
    {
        eid = new LVar<EntityId>();
        val = new LVar<THighLevel>();
        return queryBuilder.Where<Datom<THighLevel, TLowLevel>, EntityId, Attribute<THighLevel, TLowLevel>, THighLevel>(eid, attr, val);
    }
    
    public static QueryBuilder Datom<THighLevel, TLowLevel>(this QueryBuilder queryBuilder, Term<EntityId> eid, Attribute<THighLevel, TLowLevel> attr, out LVar<THighLevel> val) 
        where TLowLevel : notnull 
        where THighLevel : notnull
    {
        val = new LVar<THighLevel>();
        return queryBuilder.Where<Datom<THighLevel, TLowLevel>, EntityId, Attribute<THighLevel, TLowLevel>, THighLevel>(eid, attr, val);
    }
}
