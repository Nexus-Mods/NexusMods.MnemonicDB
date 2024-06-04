using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// Base interface for all read-only models
/// </summary>
public interface IReadOnlyModel : IHasEntityIdAndDb, IReadOnlyCollection<IReadDatom>
{
    /// <summary>
    /// Looks for the given attribute in the entity
    /// </summary>
    public bool Contains(IAttribute attribute)
    {
        foreach (var datom in this)
        {
            if (datom.A == attribute)
                return true;
        }

        return false;
    }
}

/// <summary>
/// Typed version of IReadOnlyModel, that has a static Create method
/// </summary>
public interface IReadOnlyModel<out TModel> : IReadOnlyModel
{
    /// <summary>
    /// Creates a new instance of the model with the given id
    /// </summary>
    public static abstract TModel Create(IDb db, EntityId id);
}
