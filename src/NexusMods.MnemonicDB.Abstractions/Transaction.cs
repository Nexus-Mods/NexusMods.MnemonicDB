using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NexusMods.MnemonicDB.Abstractions;

public abstract class Transaction : Datoms, IMainTransaction, ISubTransaction 
{
    private readonly Transaction? _parentTransaction;
    private IDb _asIfDb;
    private int _datomCountOfAsIf = 0;

    private readonly IConnection _connection;
    private readonly Lock _lock = new();

    private bool _disposed;
    private bool _committed;
    private readonly IDb _basisDb;

    public Transaction(IConnection connection, Transaction? parentTransaction = null) : base(connection.AttributeResolver)
    {
        _connection = connection;
        _asIfDb = connection.Db;
        _basisDb = connection.Db;
        _datomCountOfAsIf = 0;
        _parentTransaction = parentTransaction;
    }
    
    /// <summary>
    /// Retracts all datoms for the given attribute for the given entity as seen by the given db. If none are found,
    /// nothing happens
    /// </summary>
    void RetractAll(EntityId entityId, IAttribute attribute)
    {
        var ent = _basisDb[entityId];
        var aid = _basisDb.AttributeResolver.AttributeCache.GetAttributeId(attribute.Id);

        foreach (var value in ent.GetAllResolved(attribute))
        {
            this.Add(entityId, aid, value, isRetract: true);
        }
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
        _parentTransaction.AddRange(this);
    }

    /// <summary>
    /// Get the database as if this transaction was committed. This operation has a certain level of overhead
    /// and has to be recomputed after every change to the datoms list. The value is cached internally, but
    /// be aware that calling it in a loop, combined with additions will have a significant performance performance impact.
    /// </summary>
    public IDb AsIf()
    {
        lock (_lock)
        {
            if (_datomCountOfAsIf == Count)
                return _asIfDb;
            
            _asIfDb = _basisDb.AsIf(this);
            _datomCountOfAsIf = Count;
            return _asIfDb;
        }
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
        return new SubTransaction(_connection, this);
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

    public IDb BasisDb => _basisDb;
}

public class MainTransaction : Transaction
{
    public MainTransaction(IConnection connection) : base(connection)
    {
    }
}

public class SubTransaction : Transaction
{
    public SubTransaction(IConnection connection, Transaction parentTx) : base(connection, parentTx)
    {
    }
}
