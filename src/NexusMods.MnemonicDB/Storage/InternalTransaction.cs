using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.Traits;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;

namespace NexusMods.MnemonicDB.Storage;

internal class InternalTransaction(IDb basisDb, Datoms datoms) : ITransaction
{
    private ulong _tempId = PartitionId.Temp.MakeEntityId(0).Value;
    private List<ITemporaryEntity>? _temporaryEntities = null;

    /// <inheritdoc />
    public TxId ThisTxId => TxId.From(PartitionId.Temp.MakeEntityId(0).Value);

    /// <inheritdoc />
    public EntityId TempId()
    {
        return TempId(PartitionId.Entity);
    }
    
    /// <inhertdoc />
    public EntityId TempId(PartitionId partition)
    {
        var tempId = Interlocked.Increment(ref _tempId);
        // Add the partition to the id
        var actualId = ((ulong)partition.Value << 40) | tempId;
        return EntityId.From(actualId);
    }
    
    /// <inheritdoc />
    public void Add(ITxFunction fn)
    {
        fn.Apply(this, basisDb);
    }
    public void Attach(ITemporaryEntity entity)
    {
        _temporaryEntities ??= new();
        _temporaryEntities.Add(entity);
    }
    
    public bool TryGet<TEntity>(EntityId entityId, [NotNullWhen(true)] out TEntity? entity)
        where TEntity : class, ITemporaryEntity
    {
        entity = null;
        if (_temporaryEntities is null) return false;

        foreach (var tempEntity in _temporaryEntities)
        {
            if (tempEntity.Id != entityId) continue;
            if (tempEntity is not TEntity actual) continue;

            entity = actual;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public SubTransaction CreateSubTransaction()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<ICommitResult> Commit()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public void Reset()
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Writes the temporary entities to the backing index builder.
    /// </summary>
    public void ProcessTemporaryEntities()
    {
        if (_temporaryEntities is null)
            return;

        foreach (var entity in _temporaryEntities)
        {
            entity.AddTo(this);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    public List<Datom> Datoms => datoms;
    public AttributeCache AttributeCache => datoms.AttributeCache;
    public IEnumerator<Datom> GetEnumerator()
    {
        return Datoms.GetEnumerator();
    }

}
