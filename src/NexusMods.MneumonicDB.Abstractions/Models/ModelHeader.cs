namespace NexusMods.MneumonicDB.Abstractions.Models;

public struct ModelHeader
{
    public EntityId Id;
    public IDb Db;
    public ITransaction? Tx;
    public object InlineCache;

    public T[] GetReverse<TAttribute, T>()
        where TAttribute : IAttribute<EntityId>,
        new() where T : struct, IEntity
    {
        return Db.GetReverse<TAttribute, T>(Id);
    }
}
