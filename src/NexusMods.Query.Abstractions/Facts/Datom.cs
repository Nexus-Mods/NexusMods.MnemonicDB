using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Query.Abstractions.Engines;
using NexusMods.Query.Abstractions.Engines.TopDownLazy;

namespace NexusMods.Query.Abstractions.Facts;

public record struct Datom<THighLevel, TLowLevel> : IFact<EntityId, Attribute<THighLevel, TLowLevel>, THighLevel> where THighLevel : notnull
{
    public static Func<IEnumerable<ILVarBox[]>, IEnumerable<ILVarBox[]>> MakeLazyUCC(Context context, int aIdx, Attribute<THighLevel, TLowLevel> cB, THighLevel cC)
    {
        var (type, dbIdx) = context.Resolve(Term<IDb>.LVar(ConstantLVars.Db));
        
        if (type != ResolveType.LVar)
            throw new InvalidOperationException("Db is not an LVar");

        return Execute;

        IEnumerable<ILVarBox[]> Execute(IEnumerable<ILVarBox[]> stream)
        {
            foreach (var row in stream)
            {
                var db = ((LVarBox<IDb>)row[dbIdx]).Value;
                var aBox = (LVarBox<EntityId>)row[aIdx];
                foreach (var datom in db.Datoms(cB, cC))
                {
                    aBox.Value = datom.E;
                    yield return row;
                }
            }
        }
        
    }

    public static Func<IEnumerable<ILVarBox[]>, IEnumerable<ILVarBox[]>> MakeLazyLCU(Context context, int aIdx, Attribute<THighLevel, TLowLevel> cB, int cIdx)
    {
        var (type, dbIdx) = context.Resolve(Term<IDb>.LVar(ConstantLVars.Db));
        
        if (type != ResolveType.LVar)
            throw new InvalidOperationException("Db is not an LVar");

        return Execute;

        IEnumerable<ILVarBox[]> Execute(IEnumerable<ILVarBox[]> stream)
        {
            foreach (var row in stream)
            {
                var db = ((LVarBox<IDb>)row[dbIdx]).Value;
                var aBox = (LVarBox<EntityId>)row[aIdx];
                var cBox = (LVarBox<THighLevel>)row[cIdx];
                var attrId = cB.GetDbId(db.Registry.Id);
                foreach (var datom in db.Datoms(aBox.Value))
                {
                    if (datom.A != attrId)
                        continue;
                    
                    cBox.Value = cB.ReadValue(datom.ValueSpan, datom.Prefix.ValueTag, db.Registry.Id);
                    yield return row;
                }
            }
        }
    }

    public Func<object[], IEnumerable<object[]>> MakeLazy(Dictionary<ILVar, int> lvars, HashSet<ILVar> bound)
    {
        throw new NotImplementedException();
    }
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
