using System.Linq.Expressions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.TestModel;

namespace NexusMods.MnemonicDB.QueryableParser.Tests.TestHelpers;

public class QueryableModels
{
    public static IQueryable<EntityId> Entities
    {
        get
        {
            return new Queryable<EntityId>(Expression.New(typeof(EntityId).GetConstructor(new[] { typeof(int) })!, Expression.Constant(0));
        }
    }
    
    public static IQueryable<Loadout.ReadOnly> Loadouts
    {
        get
        {
            var method = typeof(Loadout).GetMethod("Query")!;
            var expr = Expression.Call(null, method, Expression.Constant(null, typeof(IConnection)));
            return new Queryable<Loadout.ReadOnly>(expr);
        }
    }
}
