using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.InternalTxFunctions;
using NexusMods.MnemonicDB.Storage.Abstractions;

namespace NexusMods.MnemonicDB.Storage;

/// <summary>
/// Implementation of the datom store
/// </summary>
public sealed partial class DatomStore : IDatomStore
{
    internal readonly IStoreBackend Backend;
    internal ISnapshot CurrentSnapshot;
    
    internal readonly ILogger<DatomStore> Logger;
    private readonly PooledMemoryBufferWriter _retractWriter;
    private readonly AttributeCache _attributeCache;
    public readonly DatomStoreSettings Settings;

    private readonly BlockingCollection<IInternalTxFunctionImpl> _pendingTransactions;
    private readonly DbStream _dbStream;
    private readonly PooledMemoryBufferWriter _writer;
    private readonly PooledMemoryBufferWriter _prevWriter;
    private TxId _asOfTx = TxId.MinValue;
    
    private IDb? _currentDb;

    private static readonly TimeSpan TransactionTimeout = TimeSpan.FromMinutes(120);
    
    private Dictionary<EntityId, Datoms> _avCache = new();

    /// <summary>
    /// Cached function to remap temporary entity ids to real entity ids
    /// </summary>
    private readonly Func<EntityId, EntityId> _remapFunc;
    
    /// <summary>
    /// Used to remap temporary entity ids to real entity ids, this is cleared after each transaction
    /// </summary>
    private Dictionary<EntityId, EntityId> _remaps = new();

    /// <summary>
    /// Cache for the next entity/tx/attribute ids
    /// </summary>
    private NextIdCache _nextIdCache;

    /// <summary>
    /// The task consuming and logging transactions
    /// </summary>
    private Thread? _loggerThread;

    private readonly CancellationTokenSource _shutdownToken = new();
    private TxId _thisTx;
    
    /// <summary>
    /// Scratch spaced to create new datoms while processing transactions
    /// </summary>
    private readonly Memory<byte> _txScratchSpace;

    private readonly TimeProvider _timeProvider;

    private enum UniqueState : byte
    {
        // We've never seen this datom before
        None = 0,
        
        // The unique datom has been asserted in the current transaction
        Asserted = 1,
        
        // The unique datom has been retracted in the current transaction
        Retracted = 2,
        
        // The unique datom has a violated constraint in the current transaction
        Violation = 3
    }
    
    /// <summary>
    /// A cache of datoms on unique attributes being processed in the current transaction. These constraints require a bit
    /// of working memory, because a single transaction could remove a datom from one entity and add it to another, and the
    /// order of these datoms in the transaction is undefined. So we may get the retraction before the assertion, or vice versa.
    /// So this dictionary contains storage for a simple state machine to check for unique constraints. Once a transaction is about
    /// to be commited we look in this dictionary for any unique constraints that have been violated and throw errors. At the start
    /// of every transaction, this dictionary is cleared.
    /// </summary>
    private SortedDictionary<Datom, UniqueState> _currentUniqueDatoms = new(UniqueAttributeEqualityComparer.Instance);

    /// <summary>
    /// DI constructor
    /// </summary>
    public DatomStore(
        ILogger<DatomStore> logger,
        DatomStoreSettings settings,
        IStoreBackend backend,
        TimeProvider? timeProvider = null,
        bool bootstrap = true)
    {
        CurrentSnapshot = default!;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _txScratchSpace = new Memory<byte>(new byte[1024]);
        _remapFunc = Remap;
        _dbStream = new DbStream();
        _attributeCache = backend.AttributeCache;
        _pendingTransactions = new BlockingCollection<IInternalTxFunctionImpl>(new ConcurrentQueue<IInternalTxFunctionImpl>());

        Backend = backend;
        _writer = new PooledMemoryBufferWriter();
        _retractWriter = new PooledMemoryBufferWriter();
        _prevWriter = new PooledMemoryBufferWriter();

        Logger = logger;
        Settings = settings;
        
        Backend.Init(settings);

        if (bootstrap) 
            Bootstrap();
    }
    
    /// <inheritdoc />
    public TxId AsOfTxId => _asOfTx;

    /// <inheritdoc />
    public AttributeCache AttributeCache => _attributeCache;

    /// <inheritdoc />
    public async Task<(StoreResult, IDb)> TransactAsync(IInternalTxFunction fn)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var casted = (IInternalTxFunctionImpl)fn;
        _pendingTransactions.Add(casted);

        var task = casted.Task;
        if (await Task.WhenAny(task, Task.Delay(TransactionTimeout)) == task)
        {
            return await task;
        }
        Logger.LogError("Transaction didn't complete after {Timeout}", TransactionTimeout);
        throw new TimeoutException($"Transaction didn't complete after {TransactionTimeout}");

    }

    /// <inheritdoc />
    public (StoreResult, IDb) Transact(IInternalTxFunction fn)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var casted = (IInternalTxFunctionImpl)fn;
        _pendingTransactions.Add(casted);

        var task = casted.Task;
        if (Task.WhenAny(task, Task.Delay(TransactionTimeout)).Result == task)
        {
            return task.Result;
        }

        Logger.LogError("Transaction didn't complete after {Timeout}", TransactionTimeout);
        throw new TimeoutException($"Transaction didn't complete after {TransactionTimeout}");
    }
    
    /// <inheritdoc />
    public IObservable<IDb> TxLog => _dbStream;

    /// <inheritdoc />
    public (StoreResult, IDb) Transact(Datoms segment)
    {
        return Transact(new IndexSegmentTransaction(segment));
    }

    /// <inheritdoc />
    public Task<(StoreResult, IDb)> TransactAsync(Datoms segment)
    {
        return TransactAsync(new IndexSegmentTransaction(segment));
    }

    /// <inheritdoc />
    public ISnapshot GetSnapshot()
    {
        return CurrentSnapshot!;
    }

    private bool _isDisposed;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;

        _shutdownToken.Cancel();
        _pendingTransactions.CompleteAdding();
        _dbStream.Dispose();
        _writer.Dispose();
        _retractWriter.Dispose();

        _isDisposed = true;
    }

    private void ConsumeTransactions()
    {
        try
        {
            while (!_pendingTransactions.IsCompleted && !_shutdownToken.Token.IsCancellationRequested)
            {
                if (!_pendingTransactions.TryTake(out var txFn, millisecondsTimeout: -1, cancellationToken: _shutdownToken.Token))
                    continue;
                try
                {
                    var result = Log(txFn);

                    var sw = Stopwatch.StartNew();
                    FinishTransaction(result, txFn);

                    var elapsed = sw.Elapsed;
                    if (elapsed >= HighPerformanceLogging.LoggingThreshold) HighPerformanceLogging.TransactionPostProcessedLong(Logger, result.AssignedTxId, elapsed);
                    else HighPerformanceLogging.TransactionPostProcessed(Logger, result.AssignedTxId, elapsed.TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    //Logger.LogError(ex, "While commiting transaction");
                    txFn.SetException(ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            try
            {
                Logger.LogError(ex, "Transaction consumer crashed");
            }
            catch (Exception)
            {
                // Do nothing, if the logger itself is not usable
            }
        }
    }

    /// <summary>
    /// Given the new store result, process the new database state, complete the transaction and notify the observers
    /// </summary>
    private void FinishTransaction(StoreResult result, IInternalTxFunctionImpl pendingTransaction)
    {
        _currentDb = _currentDb!.WithNext(result, result.AssignedTxId);
        _dbStream.OnNext(_currentDb);
        Task.Run(() => pendingTransaction.Complete(result, _currentDb));
    }

    /// <summary>
    ///     Sets up the initial state of the store.
    /// </summary>
    private void Bootstrap()
    {
        try
        {
            CurrentSnapshot = Backend.GetSnapshot();
            var lastTx = TxId.From(_nextIdCache.LastEntityInPartition(CurrentSnapshot, PartitionId.Transactions).Value);
            
            if (lastTx.Value == TxId.MinValue)
            {
                Logger.LogInformation("Bootstrapping the datom store no existing state found");
                _currentDb = CurrentSnapshot.MakeDb(TxId.MinValue, _attributeCache);
                var tx = new Datoms(_currentDb.AttributeCache);
                var internalTx = new InternalTransaction(null!, tx);
                AttributeDefinition.AddInitial(tx);
                internalTx.ProcessTemporaryEntities();
                // Call directly into `Log` as the transaction channel is not yet set up
                Log(new IndexSegmentTransaction(tx));
                CurrentSnapshot = Backend.GetSnapshot();
                _currentDb = CurrentSnapshot.MakeDb(_asOfTx, _attributeCache);
            }
            else
            {
                Logger.LogInformation("Bootstrapping the datom store, existing state found, last tx: {LastTx}",
                    lastTx.Value.ToString("x"));
                _asOfTx = TxId.From(lastTx.Value);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to bootstrap the datom store");
            throw;
        }
        
        _currentDb = CurrentSnapshot.MakeDb(_asOfTx, _attributeCache);
        _dbStream.OnNext(_currentDb);
        _loggerThread = new Thread(ConsumeTransactions)
        {
            IsBackground = true,
            Name = "MnemonicDB Transaction Logger",
        };
        _loggerThread.Start();
    }

    #region Internals

    private EntityId Remap(EntityId id)
    {
        if (id.Partition == PartitionId.Temp)
        {
            if (!_remaps.TryGetValue(id, out var newId))
            {
                if (id.Value == PartitionId.Temp.MinValue)
                {
                    var remapTo = EntityId.From(_thisTx.Value);
                    _remaps.Add(id, remapTo);
                    return remapTo;
                }
                else
                {
                    var partitionId = PartitionId.From((byte)(id.Value >> 40 & 0xFF));
                    var assignedId = _nextIdCache.NextId(CurrentSnapshot!, partitionId);
                    _remaps.Add(id, assignedId);
                    return assignedId;
                }
            }

            return newId;
        }

        return id;
    }


    private StoreResult Log(IInternalTxFunctionImpl pendingTransaction)
    {
        _thisTx = TxId.From(_nextIdCache.NextId(CurrentSnapshot, PartitionId.Transactions).Value);
        
        _remaps = new Dictionary<EntityId, EntityId>();
        
        pendingTransaction.Execute(this);
        
        return new StoreResult
        {
            AssignedTxId = _asOfTx,
            Remaps = _remaps.ToFrozenDictionary(),
            Snapshot = CurrentSnapshot
        };
    }

    internal void LogDatoms(IWriteBatch batch, Datoms datoms, bool advanceTx = true, bool enableStats = false)
    {
        var datomCount = 0;
        var swPrepare = Stopwatch.StartNew();
        var datomsSpan = CollectionsMarshal.AsSpan(datoms);
        var (retracts, asserts) = TxProcessing.NormalizeWithTxIds(datomsSpan, CurrentSnapshot!);
        
        datomCount += asserts.Count;
        
        // Retracts first
        foreach (var retract in retracts)
        {
            TxProcessing.LogRetract(batch, retract, _thisTx, AttributeCache);
        }
        
        // Asserts next
        foreach (var assert in asserts)
        {
            var withTx = assert.With(_thisTx).WithRemaps(_remapFunc);
            TxProcessing.LogAssert(batch, withTx, AttributeCache);
        }
        
        if (advanceTx) 
            LogTx(batch);
        
        batch.Commit();
        var swWrite = Stopwatch.StartNew();
        
        // Print statistics if requested
        if (enableStats)
        {
            Logger.LogDebug("{TxId} ({Count} datoms) prepared in {Elapsed}ms, written in {WriteElapsed}ms",
                _thisTx,
                datomCount,
                swPrepare.ElapsedMilliseconds - swWrite.ElapsedMilliseconds,
                swWrite.ElapsedMilliseconds);
        }

        // Advance the TX counter, if requested (default)
        if (advanceTx)
            _asOfTx = _thisTx;
        
        _avCache.Clear();
        // Update the snapshot
        CurrentSnapshot = Backend.GetSnapshot();
    }

    internal void LogDatoms(Datoms datoms, bool advanceTx = false, bool enableStats = false)
    {
        using var batch = Backend.CreateBatch();
        LogDatoms(batch, datoms, advanceTx, enableStats);
    }
    

    /// <summary>
    /// Logs the transaction entity to the batch
    /// </summary>
    /// <param name="batch"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void LogTx(IWriteBatch batch)
    {
        var id = EntityId.From(_thisTx.Value);
        var taggedValue = new TaggedValue(ValueTag.Int64, _timeProvider.GetUtcNow().UtcTicks);
        var datom = Datom.Create(id, AttributeCache.GetAttributeId(MnemonicDB.Abstractions.BuiltInEntities.Transaction.Timestamp.Id), taggedValue, _thisTx, true);
        TxProcessing.LogAssert(batch, datom, AttributeCache);
    }




    #endregion


}
