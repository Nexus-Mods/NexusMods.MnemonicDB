using System;
using System.Reactive.Linq;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// Extensions for models
/// </summary>
public static class ModelExtensions
{
    /// <summary>
    /// An observable that emits the revisions of the model, terminates when the model is deleted
    /// or otherwise invalid
    /// </summary>
    public static IObservable<TModel> Revisions<TModel>(this TModel model)
        where TModel : IReadOnlyModel<TModel>
    {
        // Pull these out of the model so that we don't keep the model data lying around in memory
        var conn = model.Db.Connection;
        var id = model.Id;
        return conn.ObserveDatoms(id)
            .QueryWhenChanged(_ => TModel.Create(conn.Db, id))
            .TakeWhile(m => m.IsValid());
    }
}
