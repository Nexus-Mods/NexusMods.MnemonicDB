using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.InternalTxFunctions;

namespace NexusMods.MnemonicDB;

/// <inheritdoc />
internal class Transaction(Connection connection) : ITransaction
{
    private readonly IndexSegmentBuilder _datoms = new(connection.AttributeCache);
    private HashSet<ITxFunction>? _txFunctions; // No reason to create the hashset if we don't need it
    private List<ITemporaryEntity>? _tempEntities;
    private ulong _tempId = PartitionId.Temp.MakeEntityId(1).Value;
    private bool _committed;
    private readonly object _lock = new();
    private IInternalTxFunction? _internalTxFunction;

    /// <inhertdoc />
    public EntityId TempId(PartitionId entityPartition)
    {
        var tempId = Interlocked.Increment(ref _tempId);
        // Add the partition to the id
        var actualId = ((ulong)entityPartition << 40) | tempId;
        return EntityId.From(actualId);
    }

    /// <inhertdoc />
    public EntityId TempId()
    {
        return TempId(PartitionId.Entity);
    }

    public void Add<TVal, TAttribute>(EntityId entityId, TAttribute attribute, TVal val, bool isRetract = false) 
        where TAttribute : IWritableAttribute<TVal>
    {
        lock (_lock)
        {
            if (_committed)
                throw new InvalidOperationException("Transaction has already been committed");

            _datoms.Add(entityId, attribute, val, ThisTxId, isRetract);
        }
    }

    public void Add<TVal, TLowLevel, TSerializer>(EntityId entityId, Attribute<TVal, TLowLevel, TSerializer> attribute, TVal val, bool isRetract = false) where TSerializer : IValueSerializer<TLowLevel> where TVal : notnull
    {
        lock (_lock)
        {
            if (_committed)
                throw new InvalidOperationException("Transaction has already been committed");

            _datoms.Add(entityId, attribute, val, ThisTxId, isRetract);
        }
    }

    public void Add(EntityId entityId, ReferencesAttribute attribute, IEnumerable<EntityId> ids)
    {
        lock (_lock)
        {
            if (_committed)
                throw new InvalidOperationException("Transaction has already been committed");

            foreach (var id in ids)
            {
                _datoms.Add(entityId, attribute, id, ThisTxId, isRetract: false);
            }
        }
    }

    /// <inheritdoc />
    public void Add(Datom datom)
    {
        lock (_lock)
        {
            _datoms.Add(datom);
        }
    }

    public void Add(ITxFunction fn)
    {
        lock (_lock)
        {
            if (_committed)
                throw new InvalidOperationException("Transaction has already been committed");

            _txFunctions ??= [];
            _txFunctions?.Add(fn);
        }
    }

    /// <summary>
    /// Sets the internal transaction function to the given function.
    /// </summary>
    public void Set(IInternalTxFunction fn)
    {
        _internalTxFunction = fn;
    }

    public void Attach(ITemporaryEntity entity)
    {
        lock (_lock)
        {
            _tempEntities ??= [];
            _tempEntities.Add(entity);
        }
    }

    public bool TryGet<TEntity>(EntityId entityId, [NotNullWhen(true)] out TEntity? entity)
        where TEntity : class, ITemporaryEntity
    {
        entity = null;
        if (_tempEntities is null) return false;

        lock (_lock)
        {
            // NOTE(erri120): this can probably be optimized,
            // as the list is very likely to be sorted by ID.
            foreach (var tempEntity in _tempEntities)
            {
                if (tempEntity.Id != entityId) continue;
                if (tempEntity is not TEntity actual) continue;

                entity = actual;
                return true;
            }
        }

        return false;
    }

    public async Task<ICommitResult> Commit()
    {
        IndexSegment built;
        lock (_lock)
        {
            if (_tempEntities != null)
            {
                foreach (var entity in _tempEntities!)
                {
                    entity.AddTo(this);
                }
            }

            _committed = true;
            // Build the datoms block here, so that future calls to add won't modify this while we're building
            built = _datoms.Build();
        }
        
        if (_internalTxFunction is not null)
            return await connection.Transact(_internalTxFunction);

        if (_txFunctions is not null) 
            return await connection.Transact(new CompoundTransaction(built, _txFunctions!) { Connection = connection });
        
        return await connection.Transact(new IndexSegmentTransaction(built));

    }

    /// <inheritdoc />
    public TxId ThisTxId => TxId.From(PartitionId.Temp.MakeEntityId(0).Value);

    public void Dispose()
    {
        _datoms.Dispose();
    }
}
