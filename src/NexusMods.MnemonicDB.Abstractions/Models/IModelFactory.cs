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

    /// <summary>
    /// Observe a model from the database, the stream ends when the model is deleted or otherwise invalidated
    /// </summary>
    public static virtual IObservable<TModel> Observe(IConnection conn, EntityId id)
    {
        return conn.ObserveDatoms(id)
            .QueryWhenChanged(d => TModel.Create(conn.Db, id))
            .TakeWhile(model => model.IsValid());
    }

    /// <summary>
    /// Observe all models of this type from the database
    /// </summary>
    public static virtual IObservable<IChangeSet<TModel>> ObserveAll(IConnection conn)
    {
        return conn.ObserveDatoms(TFactory.PrimaryAttribute)
            .Transform(d => TFactory.Load(conn.Db, d.E));
    }
}

/// <summary>
/// Extensions for IModelFactory
/// </summary>
public static class ModelFactoryExtensions
{


}
