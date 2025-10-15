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
using NexusMods.MnemonicDB.Abstractions.Traits;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.InternalTxFunctions;

namespace NexusMods.MnemonicDB;

internal sealed class Transaction : IMainTransaction, ISubTransaction 
{
    private readonly Transaction? _parentTransaction;

    private readonly Connection _connection;
    private readonly DatomList _datoms;
    private readonly Lock _lock = new();

    private bool _disposed;
    private bool _committed;

    public Transaction(Connection connection, Transaction? parentTransaction = null)
    {
        _connection = connection;
        _datoms = new DatomList(connection.AttributeCache);

        _parentTransaction = parentTransaction;
    }

    List<IDatomLikeRO> IDatomsListLike.Datoms => _datoms;

    public AttributeCache AttributeCache => _datoms.AttributeCache;

    /// <inheritdoc />
    public TxId ThisTxId => _parentTransaction?.ThisTxId ?? TxId.From(PartitionId.Temp.MakeEntityId(0).Value);

    /// <inhertdoc />
    public EntityId TempId(PartitionId entityPartition) => Abstractions.TempId.Next(entityPartition);

    /// <inhertdoc />
    public EntityId TempId() => Abstractions.TempId.Next();
    
    public void CommitToParent()
    {
        CheckAccess();
        Debug.Assert(_parentTransaction is not null);
        ((IDatomsListLike)_parentTransaction).Add(_datoms);
    }

    public async Task<ICommitResult> Commit()
    {
        CheckAccess();
        Debug.Assert(_parentTransaction is null);

        IInternalTxFunction fn;
        lock (_lock)
        {
            fn = new IndexSegmentTransaction(_datoms);
            _committed = true;
            Reset();
        }
        return await _connection.Transact(fn);
    }

    public ISubTransaction CreateSubTransaction()
    {
        return new Transaction(_connection, parentTransaction: this);
    }

    public void Reset()
    {
        _datoms.Clear();
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
