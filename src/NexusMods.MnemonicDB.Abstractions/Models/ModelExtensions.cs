using System;
using System.Reactive.Linq;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// Extensions for models
/// </summary>
public static class ModelExtensions
{
    /// <summary>
    /// Returns an observable of the revisions of the model, starting with the current version and terminating
    /// when the model is no longer valid.
    /// </summary>
    public static IObservable<TModel> Revisions<TModel>(this TModel model)
        where TModel : IReadOnlyModel<TModel>
    {
        // Extract the id so that we don't hold onto the original model (and keep its cache live)
        var id = model.Id;
        var revisions = model.Db.Connection.Revisions
            .Where(db => db.Analytics.LatestTxIds.Contains(id))
            // Start with here, so we don't filter the first Db
            .StartWith(model.Db.Connection.Db)
            .Select(db => TModel.Create(db, id))
            .TakeWhile(m => m.IsValid());
        return revisions;
    }

}
