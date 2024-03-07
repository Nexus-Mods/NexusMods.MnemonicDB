using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NexusMods.EventSourcing.Abstractions.Models;

/// <summary>
/// Abstract class for active read models.
/// </summary>
/// <typeparam name="TOuter"></typeparam>
public abstract class AActiveReadModel<TOuter> : IActiveReadModel
where TOuter : AActiveReadModel<TOuter>, IActiveReadModel
{
    private IDb _basisDb = null!;

    /// <summary>
    /// Base constructor for an active read model.
    /// </summary>
    /// <param name="basisDb"></param>
    /// <param name="id"></param>
    protected AActiveReadModel(IDb basisDb, EntityId id)
    {
        Id = id;
        BasisDb = basisDb;
        Attach((TOuter)this, basisDb.Connection);
    }

    private class Box<T>
    {
        public T? Value { get; set; }
    }

    /// <summary>
    /// Attaches the active read model to the given connection, once the last reference to the model is gone,
    /// the subscription is disposed.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="connection"></param>
    public static void Attach(TOuter model, IConnection connection)
    {
        var weakRef = new WeakReference<TOuter>(model);
        var box = new Box<IDisposable>();

        box.Value = connection.Commits.Subscribe(changes =>
        {
            if (weakRef.TryGetTarget(out var target))
            {
                target._basisDb = connection.Db;
                // TODO: This is O(n * m) on the number of datoms and the number of active read models.
                // We should probably have a map of entity ids to active read models, and some sort of
                // map if entity ids in the datom list.
                if (changes.Datoms.Any(d => d.E == target.Id))
                    connection.Db.Reload(target);
            }
            else
            {
                box.Value?.Dispose();
            }
        });

    }

    /// <summary>
    /// The current database this entity is using for its state.
    /// </summary>
    public IDb BasisDb
    {
        get => _basisDb;
        set
        {
            _basisDb = value;
            OnBasisDbChanged();
        }
    }

    private void OnBasisDbChanged()
    {
        _basisDb.Reload((TOuter)this);
    }

    /// <summary>
    /// The identifier for the entity.
    /// </summary>
    public EntityId Id { get; set; }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
