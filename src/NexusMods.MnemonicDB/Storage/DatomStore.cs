using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
using NexusMods.MnemonicDB.Abstractions.Traits;
using NexusMods.MnemonicDB.InternalTxFunctions;
using NexusMods.MnemonicDB.Storage.Abstractions;
using Reloaded.Memory.Extensions;
using static NexusMods.MnemonicDB.Abstractions.IndexType;

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
    
    private Dictionary<EntityId, IReadOnlyList<IDatomLikeRO>> _avCache = new();

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
    public (StoreResult, IDb) Transact(IReadOnlyList<IDatomLikeRO> segment)
    {
        throw new NotImplementedException();
        //return Transact(new IndexSegmentTransaction(segment));
    }

    /// <inheritdoc />
    public Task<(StoreResult, IDb)> TransactAsync(IReadOnlyList<IDatomLikeRO> segment)
    {
        throw new NotImplementedException();
        //return TransactAsync(new IndexSegmentTransaction(segment));
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
                var tx = new DatomList(_currentDb.AttributeCache);
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

    internal void LogDatoms<TSource>(TSource datoms, bool advanceTx = true, bool enableStats = false) 
        where TSource : IEnumerable<IDatomLikeRO>
    {
        var swPrepare = Stopwatch.StartNew();
        _remaps.Clear();
        _currentUniqueDatoms.Clear();
        using var batch = Backend.CreateBatch();

        var datomCount = 0;
        var dataSize = 0;
        foreach (var datom in datoms)
        {
            if (enableStats)
            {
                datomCount++;
                throw new NotImplementedException();
                //dataSize += datom.ValueSpan.Length + KeyPrefix.Size;
            }
            LogDatom(in datom, batch);
        }

        if (advanceTx) 
            LogTx(batch);
        
        CheckForUniqueViolations();
        
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
        
        _avCache.Clear();
        // Update the snapshot
        CurrentSnapshot = Backend.GetSnapshot();
    }

    /// <summary>
    /// Once we're all done processing a transaction, we need to check for any unique violations that may have occurred
    /// </summary>
    private void CheckForUniqueViolations()
    {
        foreach (var (datom, state) in _currentUniqueDatoms)
        {
            if (state == UniqueState.Violation)
                throw new UniqueConstraintException(datom);
        }
    }

    /// <summary>
    /// Log a collection of datoms to the store using the given batch. If advanceTx is true, the transaction will be advanced
    /// and this specific transaction will be considered as committed, use this in combination with other log methods
    /// to build up a single write batch and finish off with this method. 
    /// </summary>
    internal void LogDatoms<TSource>(IWriteBatch batch, TSource datoms,  bool advanceTx = false)
        where TSource : IEnumerable<Datom>
    {
        throw new NotImplementedException();
        //foreach (var datom in datoms)
        //    LogDatom(in datom, batch);
        
        if (advanceTx) 
            LogTx(batch);
        
        batch.Commit();
        
        // Advance the TX counter, if requested (not default)
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
        MemoryMarshal.Write(_txScratchSpace.Span, _timeProvider.GetUtcNow().UtcTicks);
        var id = EntityId.From(_thisTx.Value);
        var keyPrefix = new KeyPrefix(id, AttributeCache.GetAttributeId(MnemonicDB.Abstractions.BuiltInEntities.Transaction.Timestamp.Id), _thisTx, false, ValueTag.Int64);
        throw new NotImplementedException();
        /*
        var datom = new Datom(keyPrefix, _txScratchSpace[..sizeof(long)]);
        LogDatom(in datom, batch);
        */
    }

    /// <summary>
    /// Log a single datom, this is the inner loop of the transaction processing
    /// </summary>
    internal void LogDatom(in IDatomLikeRO datom, IWriteBatch batch)
    {
        _writer.Reset();

        var isRemapped = datom.E.InPartition(PartitionId.Temp);

        var currentPrefix = datom.Prefix;
        var attrId = currentPrefix.A;

        var newE = isRemapped ? Remap(currentPrefix.E) : currentPrefix.E;
        var keyPrefix = currentPrefix with {E = newE, T = _thisTx};

        {
            _writer.WriteMarshal(keyPrefix);
            throw new NotImplementedException();
            Serializer.Write(keyPrefix.ValueTag, datom.ValueObject, _writer);
            /*var valueSpan = datom.ValueSpan;
            var span = _writer.GetSpan(valueSpan.Length);
            valueSpan.CopyTo(span);
            keyPrefix.ValueTag.Remap(span, _remapFunc);
            _writer.Advance(valueSpan.Length);*/
        }

        var newSpan = _writer.AsDatom();

        ProcessMaybeUnique(attrId, newSpan);
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
                ProcessRetract(batch, attrId, _retractWriter.AsDatom(), CurrentSnapshot!);
                ProcessAssert(batch, attrId, newSpan);
                break;
        }
    }

    private void ProcessMaybeUnique(AttributeId attrId, in Datom datom)
    {
        if (!_attributeCache.IsUnique(attrId))
            return;

        var cloned = datom.Clone();
        var currentState = _currentUniqueDatoms.GetValueOrDefault(cloned, UniqueState.None);

        switch (currentState, datom.Prefix.IsRetract)
        {
            // We've never seen this unique datom before
            case (UniqueState.None, true) or (UniqueState.None, false):
            {
                // Let's see what the previous snapshot has
                var datoms = CurrentSnapshot!.Datoms(SliceDescriptor.Create(datom.Prefix.A, datom.Prefix.ValueTag, datom.ValueMemory));
                if (datoms.Any())
                {
                    // We're retracting a unique datom that already exists, another place will make sure we are retracting the 
                    // correct EntityID
                    if (datom.Prefix.IsRetract)
                    {
                        // This should really never happen
                        if (datoms.First().E != datom.E)
                            throw new InvalidOperationException("Retraction of unique datom with different entity id");
                        // Mark this unique datom as retracted
                        _currentUniqueDatoms.Add(cloned, UniqueState.Retracted);
                    }
                    else
                    {
                        // So far, it's a violation, unless we get a matching retraction
                        _currentUniqueDatoms.Add(cloned, UniqueState.Violation);
                    }
                }
                else
                {
                    _currentUniqueDatoms.Add(cloned, UniqueState.Asserted);
                }
                break;
            }
            // Asserted, then retracted. This is allowed. 
            case (UniqueState.Asserted, true):
                _currentUniqueDatoms.Remove(cloned);
                break;
            // Asserted, then reasserted. This is a violation
            case (UniqueState.Asserted, false):
                throw new UniqueConstraintException(datom);
            // Double retracted, this should never happen
            case (UniqueState.Retracted, true):
                throw new InvalidOperationException("Double retraction of a unique datom");
            // Retracted, then added. This is allowed
            case (UniqueState.Retracted, false):
                _currentUniqueDatoms[cloned] = UniqueState.Asserted;
                break;
            // A violation, then retracted. This is allowed.
            case (UniqueState.Violation, true):
                _currentUniqueDatoms.Remove(cloned);
                break;
            // A violation, then asserted. This is a violation
            case (UniqueState.Violation, false):
                throw new UniqueConstraintException(datom);
            default:
                throw new InvalidOperationException("Invalid unique state");
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

    private void ProcessRetract(IWriteBatch batch, AttributeId attrId, Datom datom, ISnapshot iterator)
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
            From = low.WithIndex(EAVTCurrent),
            To = high.WithIndex(EAVTCurrent),
            IsReverse = false
        };

        throw new NotImplementedException();
        /*
        var prevDatom = iterator.Datoms(sliceDescriptor)
            .Select(d => d.Clone())
            .FirstOrDefault();

        #if DEBUG
            // In debug mode, we perform additional checks to make sure the retraction found a datom to retract
            // The only time this fails is if the datom lookup functions `.Datoms()` are broken
            RetractionSantiyCheck(datom, prevDatom);
        #endif

        // Delete the datom from the current indexes
        batch.Delete(EAVTCurrent, prevDatom);
        batch.Delete(AEVTCurrent, prevDatom);
        if (_attributeCache.IsReference(attrId))
            batch.Delete(VAETCurrent, prevDatom);
        if (_attributeCache.IsIndexed(attrId))
            batch.Delete(AVETCurrent, prevDatom);
        
        // Put the retraction in the log
        batch.Add(IndexType.TxLog, datom);
        
        // If the attribute is a no history attribute, we don't need to put the retraction in the history indexes
        // so we can skip the rest of the processing
        if (_attributeCache.IsNoHistory(attrId))
            return;

        // Move the datom to the history index and also record the retraction
        batch.Add(EAVTHistory, prevDatom);
        batch.Add(EAVTHistory, datom);

        // Move the datom to the history index and also record the retraction
        batch.Add(AEVTHistory, prevDatom);
        batch.Add(AEVTHistory, datom);

        if (_attributeCache.IsReference(attrId))
        {
            batch.Add(VAETHistory, prevDatom);
            batch.Add(VAETHistory, datom);
        }

        if (_attributeCache.IsIndexed(attrId))
        {
            batch.Add(AVETHistory, prevDatom);
            batch.Add(AVETHistory, datom);
        }
        */
    }

    private static unsafe void RetractionSantiyCheck(Datom datom, Datom prevDatom)
    {
        Debug.Assert(prevDatom.Valid, "Previous datom should exist");
        var debugKey = prevDatom.Prefix;

        var otherPrefix = datom.Prefix;
        Debug.Assert(debugKey.E == otherPrefix.E, "Entity should match");
        Debug.Assert(debugKey.A == otherPrefix.A, "Attribute should match");

        fixed (byte* aTmp = prevDatom.ValueSpan)
        fixed (byte* bTmp = datom.ValueSpan)
        {
            var cmp = Serializer.Compare(prevDatom.Prefix.ValueTag, aTmp, prevDatom.ValueSpan.Length, otherPrefix.ValueTag, bTmp, datom.ValueSpan.Length);
            Debug.Assert(cmp == 0, "Values should match");
        }
    }

    private void ProcessAssert(IWriteBatch batch, AttributeId attributeId, Datom datom)
    {
        batch.Add(IndexType.TxLog, datom);
        batch.Add(EAVTCurrent, datom);
        batch.Add(AEVTCurrent, datom);
        if (_attributeCache.IsReference(attributeId))
            batch.Add(VAETCurrent, datom);
        if (_attributeCache.IsIndexed(attributeId))
            batch.Add(AVETCurrent, datom);
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

    private unsafe PrevState GetPreviousState(bool isRemapped, AttributeId attrId, ISnapshot snapshot, Datom toFind)
    {
        if (isRemapped) return PrevState.NotExists;

        var keyPrefix = toFind.Prefix;

        if (!_avCache.TryGetValue(toFind.E, out var cached))
        {
            cached = _currentDb!.Datoms(SliceDescriptor.Create(keyPrefix.E));
            _avCache.Add(toFind.E, cached);
        }

        throw new NotImplementedException();
        /*
        if (_attributeCache.IsCardinalityMany(attrId))
        {
            var indexOf = FirstIndexOf(cached, attrId);
            if (indexOf < 0) 
                return PrevState.NotExists;

            for (var i = indexOf; i < cached.Count; i++)
            {
                var aPrefix = cached[i].Prefix;
                var aSpan = cached[i].ValueSpan;

                fixed (byte* a = aSpan)
                fixed (byte* b = toFind.ValueSpan)
                {
                    var cmp = Serializer.Compare(aPrefix.ValueTag, a, aSpan.Length, keyPrefix.ValueTag, b, toFind.ValueSpan.Length);
                    if (cmp == 0) return PrevState.Duplicate;
                }
            }
            return PrevState.NotExists;
        }
        else
        {
            var indexOf = FirstIndexOf(cached, attrId);
            if (indexOf < 0)
                return PrevState.NotExists;
            
            var aPrefix = cached[indexOf].Prefix;
            var aSpan = cached[indexOf].ValueSpan;
            
            var bPrefix = toFind.Prefix;
            var bSpan = toFind.ValueSpan;
            fixed (byte* a = aSpan)
            fixed (byte* b = bSpan)
            {
                var cmp = Serializer.Compare(aPrefix.ValueTag, a, aSpan.Length, bPrefix.ValueTag, b, bSpan.Length);
                if (cmp == 0) return PrevState.Duplicate;
            }

            _retractWriter.Reset();
            _retractWriter.Write(cached[indexOf]);

            return PrevState.Exists;
        }
        */

    }

    private static int FirstIndexOf(IndexSegment segment, AttributeId atttrId)
    {
        for (var i = 0; i < segment.Count; i++)
            if (segment[i].A == atttrId)
                return i;
        return -1;
    }


    #endregion


}
