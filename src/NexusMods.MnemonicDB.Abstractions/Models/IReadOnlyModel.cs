using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// Base interface for all read-only models
/// </summary>
public interface IReadOnlyModel : IHasEntityIdAndDb, IReadOnlyCollection<IReadDatom>
{

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
