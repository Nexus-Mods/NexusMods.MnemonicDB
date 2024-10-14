using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using NexusMods.MnemonicDB.InternalTxFunctions;
using NexusMods.MnemonicDB.Storage.Abstractions;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Storage;

public sealed partial class DatomStore : IDatomStore
{
    internal readonly IIndex AEVTCurrent;
    internal readonly IIndex AEVTHistory;
    internal readonly IIndex AVETCurrent;
    internal readonly IIndex AVETHistory;
    internal readonly IIndex EAVTCurrent;
    internal readonly IIndex EAVTHistory;
    internal readonly IIndex VAETCurrent;
    internal readonly IIndex VAETHistory;
    internal readonly IIndex TxLogIndex;
    internal readonly IStoreBackend Backend;
    internal ISnapshot CurrentSnapshot;
    
    internal readonly ILogger<DatomStore> Logger;
    private readonly PooledMemoryBufferWriter _retractWriter;
    private readonly AttributeCache _attributeCache;
    private readonly DatomStoreSettings _settings;

    private readonly BlockingCollection<IInternalTxFunctionImpl> _pendingTransactions;
    private DbStream _dbStream;
    private readonly PooledMemoryBufferWriter _writer;
    private readonly PooledMemoryBufferWriter _prevWriter;
    private TxId _asOfTx = TxId.MinValue;

    private Task? _bootStrapTask = null;
    
    private IDb? _currentDb = null;

    private static readonly TimeSpan TransactionTimeout = TimeSpan.FromMinutes(120);

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

    private CancellationTokenSource _shutdownToken = new();
    private TxId _thisTx;
    
    /// <summary>
    /// Scratch spaced to create new datoms while processing transactions
    /// </summary>
    private readonly Memory<byte> _txScratchSpace;

    /// <summary>
    /// DI constructor
    /// </summary>
    public DatomStore(ILogger<DatomStore> logger, DatomStoreSettings settings, IStoreBackend backend, bool bootstrap = true)
    {
        CurrentSnapshot = default!;
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
        _settings = settings;
        
        Backend.DeclareEAVT(IndexType.EAVTCurrent);
        Backend.DeclareEAVT(IndexType.EAVTHistory);
        Backend.DeclareAEVT(IndexType.AEVTCurrent);
        Backend.DeclareAEVT(IndexType.AEVTHistory);
        Backend.DeclareVAET(IndexType.VAETCurrent);
        Backend.DeclareVAET(IndexType.VAETHistory);
        Backend.DeclareAVET(IndexType.AVETCurrent);
        Backend.DeclareAVET(IndexType.AVETHistory);
        Backend.DeclareTxLog(IndexType.TxLog);

        Backend.Init(settings.Path);

        TxLogIndex = Backend.GetIndex(IndexType.TxLog);
        EAVTCurrent = Backend.GetIndex(IndexType.EAVTCurrent);
        EAVTHistory = Backend.GetIndex(IndexType.EAVTHistory);
        AEVTCurrent = Backend.GetIndex(IndexType.AEVTCurrent);
        AEVTHistory = Backend.GetIndex(IndexType.AEVTHistory);
        VAETCurrent = Backend.GetIndex(IndexType.VAETCurrent);
        VAETHistory = Backend.GetIndex(IndexType.VAETHistory);
        AVETCurrent = Backend.GetIndex(IndexType.AVETCurrent);
        AVETHistory = Backend.GetIndex(IndexType.AVETHistory);

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
    public (StoreResult, IDb) Transact(IndexSegment segment)
    {
        return Transact(new IndexSegmentTransaction(segment));
    }

    /// <inheritdoc />
    public Task<(StoreResult, IDb)> TransactAsync(IndexSegment segment)
    {
        return TransactAsync(new IndexSegmentTransaction(segment));
    }

    /// <inheritdoc />
    public ISnapshot GetSnapshot()
    {
        return CurrentSnapshot!;
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        _pendingTransactions.CompleteAdding();
        _shutdownToken.Cancel();
        _loggerThread?.Join();
        _dbStream.Dispose();
        _writer.Dispose();
        _retractWriter.Dispose();
    }

    private void ConsumeTransactions()
    {
        var debugEnabled = Logger.IsEnabled(LogLevel.Debug);

        try
        {
            while (!_pendingTransactions.IsCompleted && !_shutdownToken.Token.IsCancellationRequested)
            {
                if (!_pendingTransactions.TryTake(out var txFn, -1))
                    continue;
                try
                {
                    var result = Log(txFn);

                    if (debugEnabled)
                    {
                        var sw = Stopwatch.StartNew();
                        FinishTransaction(result, txFn);
                        Logger.LogDebug("Transaction {TxId} post-processed in {Elapsed}ms", result.AssignedTxId, sw.ElapsedMilliseconds);
                    }
                    else
                    {
                        FinishTransaction(result, txFn);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "While commiting transaction");
                    txFn.SetException(ex);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Transaction consumer crashed");
        }
    }

    /// <summary>
    /// Given the new store result, process the new database state, complete the transaction and notify the observers
    /// </summary>
    private void FinishTransaction(StoreResult result, IInternalTxFunctionImpl pendingTransaction)
    {
        _currentDb = ((Db)_currentDb!).WithNext(result, result.AssignedTxId);
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
                using var builder = new IndexSegmentBuilder(_attributeCache);
                var internalTx = new InternalTransaction(null!, builder);
                AttributeDefinition.AddInitial(internalTx);
                internalTx.ProcessTemporaryEntities();
                // Call directly into `Log` as the transaction channel is not yet set up
                Log(new IndexSegmentTransaction(builder.Build()));
                CurrentSnapshot = Backend.GetSnapshot();
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
        
        _currentDb = new Db(CurrentSnapshot, _asOfTx, _attributeCache);
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
            AssignedTxId = _thisTx,
            Remaps = _remaps.ToFrozenDictionary(),
            Snapshot = CurrentSnapshot
        };
    }

    internal void LogDatoms<TSource>(TSource datoms, bool advanceTx = true, bool enableStats = false) 
        where TSource : IEnumerable<Datom>
    {
        var swPrepare = Stopwatch.StartNew();
        _remaps.Clear();
        using var batch = Backend.CreateBatch();

        var datomCount = 0;
        var dataSize = 0;
        foreach (var datom in datoms)
        {
            if (enableStats)
            {
                datomCount++;
                dataSize += datom.ValueSpan.Length + KeyPrefix.Size;
            }
            LogDatom(in datom, batch);
        }

        if (advanceTx) 
            LogTx(batch);
        
        batch.Commit();
        var swWrite = Stopwatch.StartNew();
        
        // Print statistics if requested
        if (enableStats)
        {
            Logger.LogDebug("{TxId} ({Count} datoms, {Size}) prepared in {Elapsed}ms, written in {WriteElapsed}ms",
                _thisTx,
                datomCount,
                dataSize,
                swPrepare.ElapsedMilliseconds - swWrite.ElapsedMilliseconds,
                swWrite.ElapsedMilliseconds);
        }

        // Advance the TX counter, if requested (default)
        if (advanceTx)
            _asOfTx = _thisTx;
        
        // Update the snapshot
        CurrentSnapshot = Backend.GetSnapshot();
    }

    /// <summary>
    /// Logs the transaction entity to the batch
    /// </summary>
    /// <param name="batch"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void LogTx(IWriteBatch batch)
    {
        MemoryMarshal.Write(_txScratchSpace.Span, DateTime.UtcNow.ToFileTimeUtc());
        var id = EntityId.From(_thisTx.Value);
        var keyPrefix = new KeyPrefix(id, AttributeCache.GetAttributeId(MnemonicDB.Abstractions.BuiltInEntities.Transaction.Timestamp.Id), _thisTx, false, ValueTag.Int64);
        var datom = new Datom(keyPrefix, _txScratchSpace[..sizeof(long)]);
        LogDatom(in datom, batch);
    }

    /// <summary>
    /// Log a single datom, this is the inner loop of the transaction processing
    /// </summary>
    internal void LogDatom(in Datom datom, IWriteBatch batch)
    {
        _writer.Reset();

        var isRemapped = datom.E.InPartition(PartitionId.Temp);

        var currentPrefix = datom.Prefix;
        var attrId = currentPrefix.A;

        var newE = isRemapped ? Remap(currentPrefix.E) : currentPrefix.E;
        var keyPrefix = currentPrefix with {E = newE, T = _thisTx};

        {
            _writer.WriteMarshal(keyPrefix);
            var valueSpan = datom.ValueSpan;
            var span = _writer.GetSpan(valueSpan.Length);
            valueSpan.CopyTo(span);
            keyPrefix.ValueTag.Remap(span, _remapFunc);
            _writer.Advance(valueSpan.Length);
        }

        var newSpan = _writer.GetWrittenSpan();

        if (keyPrefix.IsRetract)
        {
            ProcessRetract(batch, attrId, newSpan, CurrentSnapshot!);
            return;
        }

        switch (GetPreviousState(isRemapped, attrId, CurrentSnapshot!, newSpan))
        {
            case PrevState.Duplicate:
                return;
            case PrevState.NotExists:
                ProcessAssert(batch, attrId, newSpan);
                break;
            case PrevState.Exists:
                SwitchPrevToRetraction();
                ProcessRetract(batch, attrId, _retractWriter.GetWrittenSpan(), CurrentSnapshot!);
                ProcessAssert(batch, attrId, newSpan);
                break;
        }
    }

    /// <summary>
    /// Updates the data in _prevWriter to be a retraction of the data in that write.
    /// </summary>
    private void SwitchPrevToRetraction()
    {
        var prevKey = MemoryMarshal.Read<KeyPrefix>(_retractWriter.GetWrittenSpan());
        prevKey = prevKey with {T = _thisTx, IsRetract = true};
        MemoryMarshal.Write(_retractWriter.GetWrittenSpanWritable(), prevKey);
    }

    private void ProcessRetract(IWriteBatch batch, AttributeId attrId, ReadOnlySpan<byte> datom, ISnapshot iterator)
    {
        _prevWriter.Reset();
        _prevWriter.Write(datom);
        var prevKey = MemoryMarshal.Read<KeyPrefix>(_prevWriter.GetWrittenSpan());

        prevKey = prevKey with {T = TxId.MinValue, IsRetract = false};
        MemoryMarshal.Write(_prevWriter.GetWrittenSpanWritable(), prevKey);

        var low = new Datom(_prevWriter.GetWrittenSpan().ToArray());

        prevKey = prevKey with {T = TxId.MaxValue, IsRetract = false};
        MemoryMarshal.Write(_prevWriter.GetWrittenSpanWritable(), prevKey);
        var high = new Datom(_prevWriter.GetWrittenSpan().ToArray());

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
            // In debug mode, we perform additional checks to make sure the retraction found a datom to retract
            // The only time this fails is if the datom lookup functions `.Datoms()` are broken
            RetractionSantiyCheck(datom, prevDatom);
        #endif

        // Delete the datom from the current indexes
        EAVTCurrent.Delete(batch, prevDatom);
        AEVTCurrent.Delete(batch, prevDatom);
        if (_attributeCache.IsReference(attrId))
            VAETCurrent.Delete(batch, prevDatom);
        if (_attributeCache.IsIndexed(attrId))
            AVETCurrent.Delete(batch, prevDatom);
        
        // Put the retraction in the log
        TxLogIndex.Put(batch, datom);
        
        // If the attribute is a no history attribute, we don't need to put the retraction in the history indexes
        // so we can skip the rest of the processing
        if (_attributeCache.IsNoHistory(attrId))
            return;

        // Move the datom to the history index and also record the retraction
        EAVTHistory.Put(batch, prevDatom);
        EAVTHistory.Put(batch, datom);

        // Move the datom to the history index and also record the retraction
        AEVTHistory.Put(batch, prevDatom);
        AEVTHistory.Put(batch, datom);

        if (_attributeCache.IsReference(attrId))
        {
            VAETHistory.Put(batch, prevDatom);
            VAETHistory.Put(batch, datom);
        }

        if (_attributeCache.IsIndexed(attrId))
        {
            AVETHistory.Put(batch, prevDatom);
            AVETHistory.Put(batch, datom);
        }
    }

    private static unsafe void RetractionSantiyCheck(ReadOnlySpan<byte> datom, Datom prevDatom)
    {
        Debug.Assert(prevDatom.Valid, "Previous datom should exist");
        var debugKey = prevDatom.Prefix;

        var otherPrefix = MemoryMarshal.Read<KeyPrefix>(datom);
        Debug.Assert(debugKey.E == otherPrefix.E, "Entity should match");
        Debug.Assert(debugKey.A == otherPrefix.A, "Attribute should match");

        fixed (byte* aTmp = prevDatom.ValueSpan)
        fixed (byte* bTmp = datom.SliceFast(sizeof(KeyPrefix)))
        {
            var cmp = Serializer.Compare(prevDatom.Prefix.ValueTag, aTmp, prevDatom.ValueSpan.Length, otherPrefix.ValueTag, bTmp, datom.Length - sizeof(KeyPrefix));
            Debug.Assert(cmp == 0, "Values should match");
        }
    }

    private void ProcessAssert(IWriteBatch batch, AttributeId attributeId, ReadOnlySpan<byte> datom)
    {
        TxLogIndex.Put(batch, datom);
        EAVTCurrent.Put(batch, datom);
        AEVTCurrent.Put(batch, datom);
        if (_attributeCache.IsReference(attributeId))
            VAETCurrent.Put(batch, datom);
        if (_attributeCache.IsIndexed(attributeId))
            AVETCurrent.Put(batch, datom);
    }

    /// <summary>
    /// Used to communicate the state of a given datom
    /// </summary>
    enum PrevState
    {
        Exists,
        NotExists,
        Duplicate
    }

    private unsafe PrevState GetPreviousState(bool isRemapped, AttributeId attrId, ISnapshot snapshot, ReadOnlySpan<byte> span)
    {
        if (isRemapped) return PrevState.NotExists;

        var keyPrefix = MemoryMarshal.Read<KeyPrefix>(span);

        if (_attributeCache.IsCardinalityMany(attrId))
        {
            var sliceDescriptor = SliceDescriptor.Exact(IndexType.EAVTCurrent, span);
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
                var cmp = Serializer.Compare(found.Prefix.ValueTag, a, aSpan.Length, keyPrefix.ValueTag, b, bSpan.Length);
                return cmp == 0 ? PrevState.Duplicate : PrevState.NotExists;
            }
        }
        else
        {

            var descriptor = SliceDescriptor.Create(keyPrefix.E, keyPrefix.A);

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
                var cmp = Serializer.Compare(datom.Prefix.ValueTag, a, aSpan.Length, bPrefix.ValueTag, b, bSpan.Length);
                if (cmp == 0) return PrevState.Duplicate;
            }

            _retractWriter.Reset();
            _retractWriter.Write(datom);

            return PrevState.Exists;
        }

    }


    #endregion


}
