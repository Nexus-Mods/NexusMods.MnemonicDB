using System;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// Interface automatically implemented by source generators on IModelDefinition classes
/// </summary>
public interface IModelFactory<TFactory, TModel>
    where TFactory : IModelFactory<TFactory, TModel>
    where TModel : IReadOnlyModel<TModel>
{
    /// <summary>
    /// Loads a model from the database, not providing any validation
    /// </summary>
    public static abstract TModel Load(IDb db, EntityId id);

    /// <summary>
    /// The primary attribute of the model, this is used to detect the valid type of the model
    /// </summary>
    public static abstract IAttribute PrimaryAttribute { get; }
}
