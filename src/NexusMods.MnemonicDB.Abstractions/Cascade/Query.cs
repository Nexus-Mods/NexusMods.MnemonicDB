using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Cascade.Flows;

namespace NexusMods.MnemonicDB.Abstractions.Cascade;

public static class Query
{
    public static readonly InletDefinition<IDb> Db = new();
    
    public static readonly Flow<DbUpdate> Updates = Db.ToDbUpdate();
    
    /// <summary>
    ///  Returns a flow for a tuple where the first item is the EntityId and the other items are values pulled from the
    /// given attributes
    /// </summary>
    public static IDiffFlow<(EntityId Id, T1, T2)> Pull<T1, T2>(IReadableAttribute<T1> attr1,
        IReadableAttribute<T2> attr2)
    {
        return from d1 in attr1.QueryAll()
            join d2 in attr2.QueryAll() on d1.Id equals d2.Id
            select (d1.Id, d1.Value, d2.Value);
    }

    /// <summary>
    ///  Returns a flow for a tuple where the first item is the EntityId and the other items are values pulled from the
    /// given attributes
    /// </summary>
    public static IDiffFlow<(EntityId Id, T1, T2, T3)> Pull<T1, T2, T3>(IReadableAttribute<T1> attr1,
        IReadableAttribute<T2> attr2,
        IReadableAttribute<T3> attr3)
    {
        return from d1 in attr1.QueryAll()
            join d2 in attr2.QueryAll() on d1.Id equals d2.Id
            join d3 in attr3.QueryAll() on d1.Id equals d3.Id
            select (d1.Id, d1.Value, d2.Value, d3.Value);
    }
    
}
