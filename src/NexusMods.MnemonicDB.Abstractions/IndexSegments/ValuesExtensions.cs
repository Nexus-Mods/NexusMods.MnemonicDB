using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// Extension methods for working with values
/// </summary>
public static class ValuesExtensions
{
    /// <summary>
    /// Re-frames the view as a view of entities of the given type
    /// </summary>
    public static Entities<Values<EntityId, ulong>, TModel> As<TModel>(this Values<EntityId, ulong> ids, IDb db)
        where TModel : IModel
    {
        return new Entities<Values<EntityId, ulong>, TModel>(ids, db);
    }
}
