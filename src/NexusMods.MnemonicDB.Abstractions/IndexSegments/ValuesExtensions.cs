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
    public static Entities<Values<EntityId>, TModel> As<TModel>(this Values<EntityId> ids, IDb db)
        where TModel : IEntity
    {
        return new Entities<Values<EntityId>, TModel>(ids, db);
    }
}
