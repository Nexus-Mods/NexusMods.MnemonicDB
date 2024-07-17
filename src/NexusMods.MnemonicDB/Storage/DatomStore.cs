using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.Storage.DatomStorageStructures;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Storage;

public class DatomStore : IDatomStore
{
    private readonly IIndex _aevtCurrent;
    private readonly IIndex _aevtHistory;
    private readonly IIndex _avetCurrent;
    private readonly IIndex _avetHistory;
    private readonly IStoreBackend _backend;
    private ISnapshot? _currentSnapshot;
    private readonly IIndex _eavtCurrent;
    private readonly IIndex _eavtHistory;
    private readonly ILogger<DatomStore> _logger;
    private readonly PooledMemoryBufferWriter _retractWriter;
    private readonly AttributeRegistry _registry;
    private readonly DatomStoreSettings _settings;

    private readonly BlockingCollection<PendingTransaction> _pendingTransactions;
    private readonly IIndex _txLog;
    private BehaviorSubject<IDb>? _updatesSubject;
    private readonly IIndex _vaetCurrent;
    private readonly IIndex _vaetHistory;
    private readonly PooledMemoryBufferWriter _writer;
    private readonly PooledMemoryBufferWriter _prevWriter;
    private TxId _asOfTx = TxId.MinValue;

    private Task? _bootStrapTask = null;
    
    private IDb? _currentDb = null;

    private static readonly TimeSpan TransactionTimeout = TimeSpan.FromMinutes(120);

    /// <summary>
    /// Cache for the next entity/tx/attribute ids
    /// </summary>
    private NextIdCache _nextIdCache;

    /// <summary>
    /// The task consuming and logging transactions
    /// </summary>
    private Thread? _loggerThread;

    private CancellationTokenSource _shutdownToken = new();

    /// <summary>
    /// DI constructor
    /// </summary>
    public DatomStore(ILogger<DatomStore> logger, AttributeRegistry registry, DatomStoreSettings settings,
        IStoreBackend backend)
    {
        _pendingTransactions = new BlockingCollection<PendingTransaction>(new ConcurrentQueue<PendingTransaction>());

        _backend = backend;
        _writer = new PooledMemoryBufferWriter();
        _retractWriter = new PooledMemoryBufferWriter();
        _prevWriter = new PooledMemoryBufferWriter();


        _logger = logger;
        _settings = settings;
        _registry = registry;

        _backend.DeclareEAVT(IndexType.EAVTCurrent);
        _backend.DeclareEAVT(IndexType.EAVTHistory);
        _backend.DeclareAEVT(IndexType.AEVTCurrent);
        _backend.DeclareAEVT(IndexType.AEVTHistory);
        _backend.DeclareVAET(IndexType.VAETCurrent);
        _backend.DeclareVAET(IndexType.VAETHistory);
        _backend.DeclareAVET(IndexType.AVETCurrent);
        _backend.DeclareAVET(IndexType.AVETHistory);
        _backend.DeclareTxLog(IndexType.TxLog);

        _backend.Init(settings.Path);

        _txLog = _backend.GetIndex(IndexType.TxLog);
        _eavtCurrent = _backend.GetIndex(IndexType.EAVTCurrent);
        _eavtHistory = _backend.GetIndex(IndexType.EAVTHistory);
        _aevtCurrent = _backend.GetIndex(IndexType.AEVTCurrent);
        _aevtHistory = _backend.GetIndex(IndexType.AEVTHistory);
        _vaetCurrent = _backend.GetIndex(IndexType.VAETCurrent);
        _vaetHistory = _backend.GetIndex(IndexType.VAETHistory);
        _avetCurrent = _backend.GetIndex(IndexType.AVETCurrent);
        _avetHistory = _backend.GetIndex(IndexType.AVETHistory);

        registry.Populate(AttributeDefinition.HardcodedDbAttributes);

        Bootstrap();
    }
    
    /// <inheritdoc />
    public TxId AsOfTxId => _asOfTx;

    /// <inheritdoc />
    public IAttributeRegistry Registry => _registry;

    /// <inheritdoc />
    public async Task<(StoreResult, IDb)> TransactAsync(IndexSegment datoms, HashSet<ITxFunction>? txFunctions = null)
    {

        var pending = new PendingTransaction
        {
            Data = datoms,
            TxFunctions = txFunctions
        };
        _pendingTransactions.Add(pending);

        var task = pending.CompletionSource.Task;
        if (await Task.WhenAny(task, Task.Delay(TransactionTimeout)) == task)
        {
            return await task;
        }
        _logger.LogError("Transaction didn't complete after {Timeout}", TransactionTimeout);
        throw new TimeoutException($"Transaction didn't complete after {TransactionTimeout}");

    }

    /// <inheritdoc />
    public (StoreResult, IDb) Transact(IndexSegment datoms, HashSet<ITxFunction>? txFunctions = null)
    {

        var pending = new PendingTransaction
        {
            Data = datoms,
            TxFunctions = txFunctions
        };
        _pendingTransactions.Add(pending);

        var task = pending.CompletionSource.Task;
        if (Task.WhenAny(task, Task.Delay(TransactionTimeout)).Result == task)
        {
            return task.Result;
        }
        _logger.LogError("Transaction didn't complete after {Timeout}", TransactionTimeout);
        throw new TimeoutException($"Transaction didn't complete after {TransactionTimeout}");

    }
    
    /// <inheritdoc />
    public IObservable<IDb> TxLog
    {
        get
        {
            if (_updatesSubject == null)
                throw new InvalidOperationException("The store is not yet started");
            return _updatesSubject;
        }
    }

    /// <inheritdoc />
    public void RegisterAttributes(IEnumerable<DbAttribute> newAttrs)
    {
        var datoms = new IndexSegmentBuilder(_registry);
        var newAttrsArray = newAttrs.ToArray();

        var internalTx = new InternalTransaction(null!, datoms);
        foreach (var attribute in newAttrsArray)
            AttributeDefinition.Insert(internalTx, attribute.Attribute, attribute.AttrEntityId.Value);
        internalTx.ProcessTemporaryEntities();
        Transact(datoms.Build());

        _registry.Populate(newAttrsArray);
    }

    /// <inheritdoc />
    public ISnapshot GetSnapshot()
    {
        Debug.Assert(_currentSnapshot != null, "Current snapshot should not be null, this should never happen");
        return _currentSnapshot!;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _pendingTransactions.CompleteAdding();
        _shutdownToken.Cancel();
        _loggerThread?.Join();
        _updatesSubject?.Dispose();
        _writer.Dispose();
        _retractWriter.Dispose();
    }

    private void ConsumeTransactions()
    {
        try
        {
            while (!_pendingTransactions.IsCompleted && !_shutdownToken.Token.IsCancellationRequested)
            {
                if (!_pendingTransactions.TryTake(out var pendingTransaction, -1))
                    continue;
                try
                {
                    // Sync transactions have no data, and are used to verify that the store is up to date.
                    if (!pendingTransaction.Data.Valid && pendingTransaction.TxFunctions == null)
                    {
                        var storeResult = new StoreResult
                        {
                            Remaps = new Dictionary<EntityId, EntityId>(),
                            AssignedTxId = _nextIdCache.AsOfTxId,
                            Snapshot = _backend.GetSnapshot(),
                        };
                        pendingTransaction.Complete(storeResult, _currentDb!);
                        continue;
                    }

                    Log(pendingTransaction, out var result);
                    
                    var sw = Stopwatch.StartNew();
                    FinishTransaction(result, pendingTransaction);
                    
                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug("Transaction {TxId} post-processed in {Elapsed}ms", result.AssignedTxId, sw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "While commiting transaction");
                    pendingTransaction.CompletionSource.TrySetException(ex);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction consumer crashed");
        }
    }

    /// <summary>
    /// Given the new store result, process the new database state, complete the transaction and notify the observers
    /// </summary>
    private void FinishTransaction(StoreResult result, PendingTransaction pendingTransaction)
    {
        _currentDb = ((Db)_currentDb!).WithNext(result, result.AssignedTxId);
        _updatesSubject?.OnNext(_currentDb!);
        pendingTransaction.Complete(result, _currentDb);
    }

    /// <summary>
    ///     Sets up the initial state of the store.
    /// </summary>
    private void Bootstrap()
    {
        try
        {
            var snapshot = _backend.GetSnapshot();
            var lastTx = TxId.From(_nextIdCache.LastEntityInPartition(snapshot, PartitionId.Transactions, _registry).Value);

            if (lastTx.Value == TxId.MinValue)
            {
                _logger.LogInformation("Bootstrapping the datom store no existing state found");
                using var builder = new IndexSegmentBuilder(_registry);
                var internalTx = new InternalTransaction(null!, builder);
                AttributeDefinition.AddInitial(internalTx);
                internalTx.ProcessTemporaryEntities();
                var pending = new PendingTransaction
                {
                    Data = builder.Build(),
                    TxFunctions = null
                };
                // Call directly into `Log` as the transaction channel is not yet set up
                Log(pending, out _);
                _currentSnapshot = _backend.GetSnapshot();
            }
            else
            {
                _logger.LogInformation("Bootstrapping the datom store, existing state found, last tx: {LastTx}",
                    lastTx.Value.ToString("x"));
                _asOfTx = TxId.From(lastTx.Value);
                _currentSnapshot = snapshot;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bootstrap the datom store");
            throw;
        }
        
        _currentDb = new Db(_currentSnapshot, _asOfTx, _registry);
        _updatesSubject = new BehaviorSubject<IDb>(_currentDb);
        _loggerThread = new Thread(ConsumeTransactions)
        {
            IsBackground = true,
            Name = "MnemonicDB Transaction Logger",
        };
        _loggerThread.Start();
    }

    #region Internals

    private EntityId MaybeRemap(ISnapshot snapshot, EntityId id, Dictionary<EntityId, EntityId> remaps, TxId thisTx)
    {
        if (id.Partition == PartitionId.Temp)
        {
            if (!remaps.TryGetValue(id, out var newId))
            {
                if (id.Value == PartitionId.Temp.MinValue)
                {
                    var remapTo = EntityId.From(thisTx.Value);
                    remaps.Add(id, remapTo);
                    return remapTo;
                }
                else
                {
                    var partitionId = PartitionId.From((byte)(id.Value >> 40 & 0xFF));
                    var assignedId = _nextIdCache.NextId(snapshot, partitionId, _registry);
                    remaps.Add(id, assignedId);
                    return assignedId;
                }
            }

            return newId;
        }

        return id;
    }


    private void Log(PendingTransaction pendingTransaction, out StoreResult result)
    {
        var currentSnapshot = _currentSnapshot ?? _backend.GetSnapshot();
        var thisTx = TxId.From(_nextIdCache.NextId(currentSnapshot, PartitionId.Transactions, _registry).Value);

        var remaps = new Dictionary<EntityId, EntityId>();
        var remapFn = (Func<EntityId, EntityId>)(id => MaybeRemap(currentSnapshot, id, remaps, thisTx));
        using var batch = _backend.CreateBatch();

        var swPrepare = Stopwatch.StartNew();



        var secondaryBuilder = new IndexSegmentBuilder(_registry);
        var txId = EntityId.From(thisTx.Value);
        secondaryBuilder.Add(txId, MnemonicDB.Abstractions.BuiltInEntities.Transaction.Timestamp, DateTime.UtcNow);

        if (pendingTransaction.TxFunctions != null)
        {
            try
            {
                var db = _currentDb!;
                var tx = new InternalTransaction(db, secondaryBuilder);
                foreach (var fn in pendingTransaction.TxFunctions)
                {
                    fn.Apply(tx, db);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply transaction functions");
                throw;
            }
        }

        var secondaryData = secondaryBuilder.Build();
        var datoms = pendingTransaction.Data.Concat(secondaryData);

        foreach (var datom in datoms)
        {
            _writer.Reset();

            var isRemapped = datom.E.InPartition(PartitionId.Temp);
            var attr = _registry.GetAttribute(datom.A);

            var currentPrefix = datom.Prefix;

            var newE = isRemapped ? remapFn(currentPrefix.E) : currentPrefix.E;
            var keyPrefix = currentPrefix with {E = newE, T = thisTx};

            {
                if (attr.IsReference)
                {
                    var newV = remapFn(MemoryMarshal.Read<EntityId>(datom.ValueSpan));
                    _writer.WriteMarshal(keyPrefix);
                    _writer.WriteMarshal(newV);
                }
                else
                {
                    _writer.WriteMarshal(keyPrefix);
                    _writer.Write(datom.ValueSpan);
                }

            }

            var newSpan = _writer.GetWrittenSpan();

            if (keyPrefix.IsRetract)
            {
                ProcessRetract(batch, attr, newSpan, currentSnapshot);
                continue;
            }

            switch (GetPreviousState(isRemapped, attr, currentSnapshot, newSpan))
            {
                case PrevState.Duplicate:
                    continue;
                case PrevState.NotExists:
                    ProcessAssert(batch, attr, newSpan);
                    break;
                case PrevState.Exists:
                    SwitchPrevToRetraction(thisTx);
                    ProcessRetract(batch, attr, _retractWriter.GetWrittenSpan(), currentSnapshot);
                    ProcessAssert(batch, attr, newSpan);
                    break;
            }

        }

        var swWrite = Stopwatch.StartNew();
        batch.Commit();

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("{TxId} ({Count} datoms, {Size}) prepared in {Elapsed}ms, written in {WriteElapsed}ms",
                thisTx,
                pendingTransaction.Data.Count + secondaryData.Count,
                pendingTransaction.Data.DataSize + secondaryData.DataSize,
                swPrepare.ElapsedMilliseconds - swWrite.ElapsedMilliseconds,
                swWrite.ElapsedMilliseconds);

        _asOfTx = thisTx;

        _currentSnapshot = _backend.GetSnapshot();
        result = new StoreResult
        {
            AssignedTxId = thisTx,
            Remaps = remaps,
            Snapshot = _currentSnapshot
        };
    }

    /// <summary>
    /// Updates the data in _prevWriter to be a retraction of the data in that write.
    /// </summary>
    /// <param name="thisTx"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void SwitchPrevToRetraction(TxId thisTx)
    {
        var prevKey = MemoryMarshal.Read<KeyPrefix>(_retractWriter.GetWrittenSpan());
        prevKey = prevKey with {T = thisTx, IsRetract = true};
        MemoryMarshal.Write(_retractWriter.GetWrittenSpanWritable(), prevKey);
    }

    private void ProcessRetract(IWriteBatch batch, IAttribute attribute, ReadOnlySpan<byte> datom, ISnapshot iterator)
    {
        _prevWriter.Reset();
        _prevWriter.Write(datom);
        var prevKey = MemoryMarshal.Read<KeyPrefix>(_prevWriter.GetWrittenSpan());

        prevKey = prevKey with {T = TxId.MinValue, IsRetract = false};
        MemoryMarshal.Write(_prevWriter.GetWrittenSpanWritable(), prevKey);

        var low = new Datom(_prevWriter.GetWrittenSpan().ToArray(), Registry);

        prevKey = prevKey with {T = TxId.MaxValue, IsRetract = false};
        MemoryMarshal.Write(_prevWriter.GetWrittenSpanWritable(), prevKey);
        var high = new Datom(_prevWriter.GetWrittenSpan().ToArray(), Registry);

        var sliceDescriptor = new SliceDescriptor
        {
            Index = IndexType.EAVTCurrent,
            From = low,
            To = high
        };

        var prevDatom = iterator.Datoms(sliceDescriptor)
            .Select(d => d.Clone())
            .FirstOrDefault();

        #if DEBUG
        unsafe
        {
            Debug.Assert(prevDatom.Valid, "Previous datom should exist");
            var debugKey = prevDatom.Prefix;

            var otherPrefix = MemoryMarshal.Read<KeyPrefix>(datom);
            Debug.Assert(debugKey.E == otherPrefix.E, "Entity should match");
            Debug.Assert(debugKey.A == otherPrefix.A, "Attribute should match");

            fixed (byte* aTmp = prevDatom.ValueSpan)
            fixed (byte* bTmp = datom.SliceFast(sizeof(KeyPrefix)))
            {
                var valueTag = prevDatom.Prefix.ValueTag;
                var cmp = ValueComparer.CompareValues(prevDatom.Prefix.ValueTag, aTmp, prevDatom.ValueSpan.Length, otherPrefix.ValueTag, bTmp, datom.Length - sizeof(KeyPrefix));
                Debug.Assert(cmp == 0, "Values should match");
            }
        }
        #endif

        _eavtCurrent.Delete(batch, prevDatom);
        _aevtCurrent.Delete(batch, prevDatom);
        if (attribute.IsReference)
            _vaetCurrent.Delete(batch, prevDatom);
        if (attribute.IsIndexed)
            _avetCurrent.Delete(batch, prevDatom);

        _txLog.Put(batch, datom);
        if (attribute.NoHistory) return;

        // Move the datom to the history index and also record the retraction
        _eavtHistory.Put(batch, prevDatom);
        _eavtHistory.Put(batch, datom);

        // Move the datom to the history index and also record the retraction
        _aevtHistory.Put(batch, prevDatom);
        _aevtHistory.Put(batch, datom);

        if (attribute.IsReference)
        {
            _vaetHistory.Put(batch, prevDatom);
            _vaetHistory.Put(batch, datom);
        }

        if (attribute.IsIndexed)
        {
            _avetHistory.Put(batch, prevDatom);
            _avetHistory.Put(batch, datom);
        }
    }

    private void ProcessAssert(IWriteBatch batch, IAttribute attribute, ReadOnlySpan<byte> datom)
    {
        _txLog.Put(batch, datom);
        _eavtCurrent.Put(batch, datom);
        _aevtCurrent.Put(batch, datom);
        if (attribute.IsReference)
            _vaetCurrent.Put(batch, datom);
        if (attribute.IsIndexed)
            _avetCurrent.Put(batch, datom);
    }

    enum PrevState
    {
        Exists,
        NotExists,
        Duplicate
    }

    private unsafe PrevState GetPreviousState(bool isRemapped, IAttribute attribute, ISnapshot snapshot, ReadOnlySpan<byte> span)
    {
        if (isRemapped) return PrevState.NotExists;

        var keyPrefix = MemoryMarshal.Read<KeyPrefix>(span);

        if (attribute.Cardinalty == Cardinality.Many)
        {
            var sliceDescriptor = SliceDescriptor.Exact(IndexType.EAVTCurrent, span, _registry);
            var found = snapshot.Datoms(sliceDescriptor)
                .FirstOrDefault();
            if (!found.Valid) return PrevState.NotExists;
            if (found.E != keyPrefix.E || found.A != keyPrefix.A)
                return PrevState.NotExists;

            var aSpan = found.ValueSpan;
            var bSpan = span.SliceFast(sizeof(KeyPrefix));
            fixed (byte* a = aSpan)
            fixed (byte* b = bSpan)
            {
                var cmp = ValueComparer.CompareValues(found.Prefix.ValueTag, a, aSpan.Length, keyPrefix.ValueTag, b, bSpan.Length);
                return cmp == 0 ? PrevState.Duplicate : PrevState.NotExists;
            }
        }
        else
        {

            var descriptor = SliceDescriptor.Create(keyPrefix.E, keyPrefix.A, _registry);

            var datom = snapshot.Datoms(descriptor)
                .FirstOrDefault();
            if (!datom.Valid) return PrevState.NotExists;

            var currKey = datom.Prefix;
            if (currKey.E != keyPrefix.E || currKey.A != keyPrefix.A)
                return PrevState.NotExists;

            var aSpan = datom.ValueSpan;
            var bSpan = span.SliceFast(sizeof(KeyPrefix));
            var bPrefix = MemoryMarshal.Read<KeyPrefix>(span);
            fixed (byte* a = aSpan)
            fixed (byte* b = bSpan)
            {
                var cmp = ValueComparer.CompareValues(datom.Prefix.ValueTag, a, aSpan.Length, bPrefix.ValueTag, b, bSpan.Length);
                if (cmp == 0) return PrevState.Duplicate;
            }

            _retractWriter.Reset();
            _retractWriter.Write(datom);

            return PrevState.Exists;
        }

    }


    #endregion

}
