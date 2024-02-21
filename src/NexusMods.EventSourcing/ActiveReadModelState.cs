using System;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;

namespace NexusMods.EventSourcing;

internal abstract class ActiveReadModelState
{
    /// <summary>
    /// The type of this read model
    /// </summary>
    public abstract Type ReadModelType { get; }
}

internal class ActiveReadModelState<TReadModel>
    where TReadModel : AReadModel<TReadModel>
{
    public Type ReadModelType => typeof(TReadModel);

    private WeakReference<TReadModel> _reference;
    private readonly Action<TReadModel,IEntityIterator,IDb> _readerFn;

    public ActiveReadModelState(TReadModel inputModel, Action<TReadModel, IEntityIterator, IDb> readerFn)
    {
        _reference = new WeakReference<TReadModel>(inputModel);
        _readerFn = readerFn;
    }

    public bool Handle(IEntityIterator iterator, IDb db)
    {
        if (_reference.TryGetTarget(out var model))
        {
            iterator.Set(model.Id);
            _readerFn(model, iterator, db);
            return true;
        }
        return false;
    }
}
