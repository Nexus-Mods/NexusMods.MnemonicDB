using System;
using System.Collections.Generic;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB;

public class Repository<T>(IConnection conn, IAttribute[] requiredAttributes) : IRepository<T>
where T : ReadOnlyModel, INewableReadOnlyModel<T>
{
    /// <inheritdoc />
    public T Create(IDb db, EntityId id)
        => T.CreateReadOnly(db, id);


    /// <inheritdoc />
    public Type RepositoryType => typeof(T);

    /// <inheritdoc />
    public bool TryGet(EntityId id, out T model)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IEnumerable<T> All()
    {
        var db = conn.Db;
        var items = db.Find(requiredAttributes[0]);
        foreach (var attr in requiredAttributes[1..])
        {
            items = items.Intersect(db.Find(attr));
        }
        return items.Select(id => db.Get<T>(id));
    }

    public IEnumerable<T> Where(IAttribute<THigher, TLower> attr, THigher value)
    {
        throw new NotImplementedException();
    }
}
