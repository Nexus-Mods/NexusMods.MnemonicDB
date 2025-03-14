using System;
using DynamicData;
using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.MnemonicDB.Abstractions.Cascade;

public static class Query
{
    public static readonly Inlet<IDb> Db = new();
    public static IQuery<EntityId> Where<T>(IWritableAttribute<T> attr, T value)
    {
        return from db in Db
            from d in db.Datoms(attr, value)
            select d.E;
    }

    public static IQuery<EntityId> Where(IAttribute attr)
    {
        throw new NotImplementedException();
    }

    public static IQuery<(EntityId Id, T1 V)> Select<T1>(IReadableAttribute<T1> a1)
    {
        return from db in Db
            from d in db.Datoms(a1)
            select (d.E, a1.ReadValue(d.ValueSpan, d.Prefix.ValueTag, db.Connection.AttributeResolver));
    }
    
    
    public static IQuery<(EntityId, T1, T2)> Select<T1, T2>(this IQuery<EntityId> query, IReadableAttribute<T1> a1, IReadableAttribute<T2> a2)
    {
        return from eid in query
            join a1v in Select(a1) on eid equals a1v.Id
            join a2v in Select(a2) on eid equals a2v.Id
            select (eid, a1v.V, a2v.V);
    }

    public static IQuery<EntityId> Where<T>(this IQuery<EntityId> id, IWritableAttribute<T> attr, T value)
    {
        throw new NotImplementedException();
    }
}
