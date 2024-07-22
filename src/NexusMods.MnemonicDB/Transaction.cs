using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.MnemonicDB;

/// <inheritdoc />
internal class Transaction(Connection connection, IAttributeRegistry registry) : ITransaction
{
    private readonly IndexSegmentBuilder _datoms = new(registry);
    private HashSet<ITxFunction>? _txFunctions = null; // No reason to create the hashset if we don't need it
    private List<ITemporaryEntity>? _tempEntities = null;
    private ulong _tempId = PartitionId.Temp.MakeEntityId(1).Value;
    private bool _committed = false;
    private readonly object _lock = new();

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

    public void Add<TVal, TLowLevel>(EntityId entityId, Attribute<TVal, TLowLevel> attribute, TVal val, bool isRetract = false)
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

    public void Attach(ITemporaryEntity entity)
    {
        lock (_lock)
        {
            _tempEntities ??= [];
            _tempEntities.Add(entity);
        }
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
        return await connection.Transact(built, _txFunctions);
    }

    /// <inheritdoc />
    public TxId ThisTxId => TxId.From(PartitionId.Temp.MakeEntityId(0).Value);

    public void Dispose()
    {
        _datoms.Dispose();
    }
}
