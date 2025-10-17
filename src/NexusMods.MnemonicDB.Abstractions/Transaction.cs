using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NexusMods.MnemonicDB.Abstractions;

public abstract class ATransaction : Datoms, IMainTransaction, ISubTransaction 
{
    private readonly ATransaction? _parentTransaction;

    private readonly IConnection _connection;
    private readonly Lock _lock = new();

    private bool _disposed;
    private bool _committed;

    public ATransaction(IConnection connection, ATransaction? parentTransaction = null) : base(connection.AttributeCache)
    {
        _connection = connection;
        _parentTransaction = parentTransaction;
    }
    
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
        throw new NotImplementedException();
        //(_parentTransaction).Add(_datoms);
    }

    public async Task<ICommitResult> Commit()
    {
        CheckAccess();
        Debug.Assert(_parentTransaction is null);

        var result = await _connection.Commit(this);
        lock (_lock)
        {
            _committed = true;
            Reset();
        }
        return result;
    }

    public SubTransaction CreateSubTransaction()
    {
        throw new NotImplementedException();
        //return new Transaction(_connection, parentTransaction: this);
    }

    public void Reset()
    {
        Clear();
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

    public IEnumerator<Datom> GetEnumerator()
    {
        return GetEnumerator();
    }

}

public class MainTransaction : ATransaction
{
    public MainTransaction(IConnection connection) : base(connection)
    {
    }
}

public class SubTransaction : ATransaction
{
    public SubTransaction(IConnection connection, ATransaction parentTx) : base(connection, parentTx)
    {
    }
}
