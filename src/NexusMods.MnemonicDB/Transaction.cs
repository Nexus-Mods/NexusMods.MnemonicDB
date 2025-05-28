using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.InternalTxFunctions;

namespace NexusMods.MnemonicDB;

internal sealed class Transaction : IMainTransaction, ISubTransaction 
{
    private readonly Transaction? _parentTransaction;

    private readonly Connection _connection;
    private readonly IndexSegmentBuilder _datoms;
    private readonly Lock _lock = new();

    private HashSet<ITxFunction>? _txFunctions;
    private List<ITemporaryEntity>? _tempEntities;
    private IInternalTxFunction? _internalTxFunction;

    private bool _disposed;
    private bool _committed;
    private ulong _tempId = PartitionId.Temp.MakeEntityId(1).Value;

    public Transaction(Connection connection, Transaction? parentTransaction = null)
    {
        _connection = connection;
        _datoms = new IndexSegmentBuilder(connection.AttributeCache);

        _parentTransaction = parentTransaction;
    }

    /// <inheritdoc />
    public TxId ThisTxId => _parentTransaction?.ThisTxId ?? TxId.From(PartitionId.Temp.MakeEntityId(0).Value);

    /// <inhertdoc />
    public EntityId TempId(PartitionId entityPartition)
    {
        if (_parentTransaction is not null) return _parentTransaction.TempId(entityPartition);

        var tempId = Interlocked.Increment(ref _tempId);
        // Add the partition to the id
        var actualId = ((ulong)entityPartition << 40) | tempId;
        return EntityId.From(actualId);
    }

    /// <inhertdoc />
    public EntityId TempId()
    {
        if (_parentTransaction is not null) return _parentTransaction.TempId();
        return TempId(PartitionId.Entity);
    }

    public void Add<TVal, TAttribute>(EntityId entityId, TAttribute attribute, TVal val, bool isRetract = false) 
        where TAttribute : IWritableAttribute<TVal>
    {
        lock (_lock)
        {
            CheckAccess();
            _datoms.Add(entityId, attribute, val, ThisTxId, isRetract);
        }
    }

    public void Add<TVal, TLowLevel, TSerializer>(EntityId entityId, Attribute<TVal, TLowLevel, TSerializer> attribute, TVal val, bool isRetract = false) where TSerializer : IValueSerializer<TLowLevel> where TVal : notnull
    {
        lock (_lock)
        {
            CheckAccess();
            _datoms.Add(entityId, attribute, val, ThisTxId, isRetract);
        }
    }

    public void Add(EntityId entityId, ReferencesAttribute attribute, IEnumerable<EntityId> ids)
    {
        lock (_lock)
        {
            CheckAccess();
            foreach (var id in ids)
            {
                _datoms.Add(entityId, attribute, id, ThisTxId, isRetract: false);
            }
        }
    }

    public void Add(EntityId e, AttributeId a, ValueTag valueTag, ReadOnlySpan<byte> valueSpan, bool isRetract = false)
    {
        lock (_lock)
        {
            CheckAccess();
            _datoms.Add(e, a, valueTag, valueSpan, isRetract);
        }
    }

    /// <inheritdoc />
    public void Add(Datom datom)
    {
        lock (_lock)
        {
            CheckAccess();
            _datoms.Add(datom);
        }
    }

    public void Add(ITxFunction fn)
    {
        lock (_lock)
        {
            CheckAccess();

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
            CheckAccess();
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

    public void CommitToParent()
    {
        CheckAccess();
        Debug.Assert(_parentTransaction is not null);

        var indexSegment = _datoms.Build();
        foreach (var datom in indexSegment)
        {
            _parentTransaction.Add(datom);
        }

        if (_tempEntities is not null)
        {
            foreach (var tmpEntity in _tempEntities)
            {
                _parentTransaction.Attach(tmpEntity);
            }
        }

        if (_txFunctions is not null)
        {
            foreach (var txFunction in _txFunctions)
            {
                _parentTransaction.Add(txFunction);
            }
        }

        _committed = true;
    }

    public async Task<ICommitResult> Commit()
    {
        CheckAccess();
        Debug.Assert(_parentTransaction is null);

        IndexSegment built;
        lock (_lock)
        {
            if (_tempEntities is not null)
            {
                foreach (var entity in _tempEntities)
                {
                    entity.AddTo(this);
                }
            }

            _committed = true;

            // Build the datoms block here, so that future calls to add won't modify this while we're building
            built = _datoms.Build();
        }
        
        if (_internalTxFunction is not null)
            return await _connection.Transact(_internalTxFunction);

        if (_txFunctions is not null) 
            return await _connection.Transact(new CompoundTransaction(built, _txFunctions) { Connection = _connection });
        
        return await _connection.Transact(new IndexSegmentTransaction(built));
    }

    public ISubTransaction CreateSubTransaction()
    {
        return new Transaction(_connection, parentTransaction: this);
    }

    public void Reset()
    {
        _datoms.Reset();

        _tempEntities?.Clear();
        _tempEntities = null;

        _txFunctions?.Clear();
        _txFunctions = null;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        Reset();
    }

    private void CheckAccess()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_committed) throw new InvalidOperationException("Transaction has already been committed!");
    }
}
