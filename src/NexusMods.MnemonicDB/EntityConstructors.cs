using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB;

internal static class EntityConstructors<TType>
{

    public static readonly Func<EntityId, IDb, TType> Constructor = MakeEntityConstructor();

    private static Func<EntityId, IDb, TType> MakeEntityConstructor()
    {
        var type = typeof(TType);
        var eidParam = Expression.Parameter(typeof(EntityId), "eid");
        var dbParam = Expression.Parameter(typeof(IDb), "db");

        var constructor = typeof(TType).GetConstructor([typeof(ITransaction)]);
        if (constructor == null)
            throw new InvalidOperationException($"Entity {type.FullName} does not have a constructor that takes an ITransaction");
        var newExpr = Expression.New(constructor, Expression.Constant(null, typeof(ITransaction)));

        var block = new List<Expression>();
        var local = Expression.Variable(type, "entity");
        block.Add(Expression.Assign(local, newExpr));

        block.Add(Expression.Assign(Expression.Property(local, "Id"), eidParam));
        block.Add(Expression.Assign(Expression.Property(local, "Db"), dbParam));

        block.Add(local);

        var lambda = Expression.Lambda<Func<EntityId, IDb, TType>>
            (Expression.Block(new[] { local }, block), eidParam, dbParam);

        return lambda.Compile();
    }
}
