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
        where TModel : IReadOnlyModel<TModel>
    {
        return new Entities<Values<EntityId, ulong>, TModel>(ids, db);
    }

    /// <summary>
    /// Returns a view of the values as models whose ids are the given id
    /// </summary>
    public static ValueEntities<TModel> AsModels<TModel>(this Values<EntityId, ulong> values, IDb db)
        where TModel : IReadOnlyModel<TModel>
    {
        return new ValueEntities<TModel>(values, db);
    }
}
