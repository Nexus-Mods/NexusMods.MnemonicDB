using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Internals;
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
    private readonly IIndex _eavtCurrent;
    private readonly IIndex _eavtHistory;
    private readonly ILogger<DatomStore> _logger;
    private readonly PooledMemoryBufferWriter _retractWriter;
    private readonly AttributeRegistry _registry;
    private readonly DatomStoreSettings _settings;
    private readonly Channel<PendingTransaction> _txChannel;
    private readonly IIndex _txLog;
    private readonly Subject<(TxId TxId, ISnapshot snapshot)> _updatesSubject;
    private readonly IIndex _vaetCurrent;
    private readonly IIndex _vaetHistory;
    private readonly PooledMemoryBufferWriter _writer;
    private readonly PooledMemoryBufferWriter _prevWriter;

    private TxId _asOfTxId = TxId.MinValue;
    private EntityId _nextEntityId;
    private readonly Task _startupTask;
    /// <summary>
    /// Cached version of the registry ID to avoid the overhead of looking it up every time
    /// </summary>
    private readonly RegistryId _registryId;


    public DatomStore(ILogger<DatomStore> logger, AttributeRegistry registry, DatomStoreSettings settings,
        IStoreBackend backend)
    {
        _backend = backend;
        _writer = new PooledMemoryBufferWriter();
        _retractWriter = new PooledMemoryBufferWriter();
        _prevWriter = new PooledMemoryBufferWriter();


        _logger = logger;
        _settings = settings;
        _registry = registry;
        _registryId = registry.Id;
        _nextEntityId = EntityId.From(Ids.MinId(Ids.Partition.Entity) + 1);

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


        _updatesSubject = new Subject<(TxId TxId, ISnapshot Snapshot)>();

        registry.Populate(BuiltInAttributes.Initial);

        _txChannel = Channel.CreateUnbounded<PendingTransaction>();
        _startupTask = Bootstrap();
        Task.Run(ConsumeTransactions);
    }

    /// <inheritdoc />
    public TxId AsOfTxId => _asOfTxId;

    /// <inheritdoc />
    public IAttributeRegistry Registry => _registry;


    /// <inheritdoc />
    public async Task<StoreResult> Transact(IndexSegment datoms)
    {
        var pending = new PendingTransaction { Data = datoms };
        if (!_txChannel.Writer.TryWrite(pending))
            throw new InvalidOperationException("Failed to write to the transaction channel");

        return await pending.CompletionSource.Task;
    }

    public IObservable<(TxId TxId, ISnapshot Snapshot)> TxLog => _updatesSubject;

    /// <inheritdoc />
    public async Task RegisterAttributes(IEnumerable<DbAttribute> newAttrs)
    {
        var datoms = new IndexSegmentBuilder(_registry);
        var newAttrsArray = newAttrs.ToArray();

        foreach (var attr in newAttrsArray)
        {
            datoms.Add(EntityId.From(attr.AttrEntityId.Value), BuiltInAttributes.UniqueId, attr.UniqueId, TxId.Tmp, false);
            datoms.Add(EntityId.From(attr.AttrEntityId.Value), BuiltInAttributes.ValueType, attr.LowLevelType, TxId.Tmp, false);
        }

        await Transact(datoms.Build());

        _registry.Populate(newAttrsArray);
    }

    /// <inheritdoc />
    public ISnapshot GetSnapshot()
    {
        if (!_startupTask.IsCompleted)
            _startupTask.Wait();
        return _backend.GetSnapshot();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _updatesSubject.Dispose();
        _writer.Dispose();
        _retractWriter.Dispose();
    }

    private async Task ConsumeTransactions()
    {
        try
        {
            while (await _txChannel.Reader.WaitToReadAsync())
            {
                var pendingTransaction = await _txChannel.Reader.ReadAsync();
                try
                {
                    // Sync transactions have no data, and are used to verify that the store is up to date.
                    if (!pendingTransaction.Data.Valid)
                    {
                        var storeResult = new StoreResult
                        {
                            Remaps = new Dictionary<EntityId, EntityId>(),
                            AssignedTxId = _asOfTxId,
                            Snapshot = null!,
                        };
                        pendingTransaction.CompletionSource.TrySetResult(storeResult);
                        continue;
                    }

                    Log(pendingTransaction, out var result);

                    _updatesSubject.OnNext((result.AssignedTxId, result.Snapshot));
                    pendingTransaction.CompletionSource.TrySetResult(result);
                }
                catch (Exception ex)
                {
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
    ///     Sets up the initial state of the store.
    /// </summary>
    private async Task Bootstrap()
    {
        try
        {
            var snapshot = _backend.GetSnapshot();
            var lastTx = snapshot.Datoms(IndexType.TxLog, TxId.MaxValue, TxId.MinValue)
                .Select(d => d.T)
                .FirstOrDefault(TxId.MinValue);

            if (lastTx == TxId.MinValue)
            {
                _logger.LogInformation("Bootstrapping the datom store no existing state found");
                var _ = await Transact(BuiltInAttributes.InitialDatoms(_registry));
                return;
            }

            _logger.LogInformation("Bootstrapping the datom store, existing state found, last tx: {LastTx}",
                lastTx.Value.ToString("x"));
            _asOfTxId = lastTx;

            var lastEnt = snapshot.Datoms(IndexType.EAVTCurrent, EntityId.MaxValueNoPartition, EntityId.MinValueNoPartition)
                .Select(e => e.E)
                .FirstOrDefault(EntityId.MinValue);

            _nextEntityId = EntityId.From(lastEnt.Value + 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bootstrap the datom store");
        }
    }

    #region Internals

    private EntityId MaybeRemap(EntityId id, Dictionary<EntityId, EntityId> remaps, TxId thisTx)
    {
        if (Ids.GetPartition(id) == Ids.Partition.Tmp)
        {
            if (!remaps.TryGetValue(id, out var newId))
            {
                if (id.Value == Ids.MinId(Ids.Partition.Tmp))
                {
                    var remapTo = EntityId.From(thisTx.Value);
                    remaps.Add(id, remapTo);
                    return remapTo;
                }
                else
                {
                    remaps.Add(id, _nextEntityId);
                    var remapTo = _nextEntityId;
                    _nextEntityId = EntityId.From(_nextEntityId.Value + 1);
                    return remapTo;
                }
            }

            return newId;
        }

        return id;
    }


    private void Log(PendingTransaction pendingTransaction, out StoreResult result)
    {
        var thisTx = TxId.From(_asOfTxId.Value + 1);

        var remaps = new Dictionary<EntityId, EntityId>();
        var remapFn = (Func<EntityId, EntityId>)(id => MaybeRemap(id, remaps, thisTx));
        using var batch = _backend.CreateBatch();

        var swPrepare = Stopwatch.StartNew();

        var currentSnapshot = _backend.GetSnapshot();

        var secondaryBuilder = new IndexSegmentBuilder(_registry);
        var txId = EntityId.From(thisTx.Value);
        secondaryBuilder.Add(txId, BuiltInAttributes.TxTimestanp,
            DateTime.UtcNow);

        var datoms = pendingTransaction.Data.Concat(secondaryBuilder.Build());

        foreach (var datom in datoms)
        {
            _writer.Reset();

            var isRemapped = Ids.IsPartition(datom.E.Value, Ids.Partition.Tmp);
            var attr = _registry.GetAttribute(datom.A);

            var currentPrefix = datom.Prefix;

            var newE = isRemapped ? remapFn(currentPrefix.E) : currentPrefix.E;
            var keyPrefix = new KeyPrefix().Set(newE, currentPrefix.A, thisTx, currentPrefix.IsRetract);

            {
                if (attr.IsReference)
                {
                    var newV = remapFn(MemoryMarshal.Read<EntityId>(datom.ValueSpan.SliceFast(1)));
                    _writer.WriteMarshal(keyPrefix);
                    _writer.WriteMarshal((byte)ValueTags.Reference);
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
            _logger.LogDebug("Transaction {TxId} ({Count} datoms) prepared in {Elapsed}ms, written in {WriteElapsed}ms",
                thisTx.Value,
                pendingTransaction.Data.Count,
                swPrepare.ElapsedMilliseconds - swWrite.ElapsedMilliseconds,
                swWrite.ElapsedMilliseconds);


        result = new StoreResult
        {
            AssignedTxId = thisTx,
            Remaps = remaps,
            Snapshot = _backend.GetSnapshot()
        };
        _asOfTxId = thisTx;
    }

    /// <summary>
    /// Updates the data in _prevWriter to be a retraction of the data in that write.
    /// </summary>
    /// <param name="thisTx"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void SwitchPrevToRetraction(TxId thisTx)
    {
        var prevKey = MemoryMarshal.Read<KeyPrefix>(_retractWriter.GetWrittenSpan());
        var (e, a, _, _) = prevKey;
        prevKey.Set(e, a, thisTx, true);
        MemoryMarshal.Write(_retractWriter.GetWrittenSpanWritable(), prevKey);
    }

    private void ProcessRetract(IWriteBatch batch, IAttribute attribute, ReadOnlySpan<byte> datom, ISnapshot iterator)
    {
        _prevWriter.Reset();
        _prevWriter.Write(datom);
        var prevKey = MemoryMarshal.Read<KeyPrefix>(_prevWriter.GetWrittenSpan());
        var (e, a, _, _) = prevKey;
        prevKey.Set(e, a, TxId.MinValue, false);
        MemoryMarshal.Write(_prevWriter.GetWrittenSpanWritable(), prevKey);

        var prevDatom = iterator.Datoms(IndexType.EAVTCurrent, _prevWriter.GetWrittenSpan())
            .Select(d => d.Clone())
            .FirstOrDefault();

        #if DEBUG
        unsafe
        {
            Debug.Assert(prevDatom.Valid, "Previous datom should exist");
            var debugKey = prevDatom.Prefix;
            Debug.Assert(debugKey.E == MemoryMarshal.Read<KeyPrefix>(datom).E, "Entity should match");
            Debug.Assert(debugKey.A == MemoryMarshal.Read<KeyPrefix>(datom).A, "Attribute should match");

            fixed (byte* aTmp = prevDatom.ValueSpan)
            fixed (byte* bTmp = datom.SliceFast(sizeof(KeyPrefix)))
            {
                var cmp = ValueComparer.CompareValues(aTmp, prevDatom.ValueSpan.Length, bTmp, datom.Length - sizeof(KeyPrefix));
                Debug.Assert(cmp == 0, "Values should match");
            }
        }
        #endif

        _eavtCurrent.Delete(batch, prevDatom.RawSpan);
        _aevtCurrent.Delete(batch, prevDatom.RawSpan);
        if (attribute.IsReference)
            _vaetCurrent.Delete(batch, prevDatom.RawSpan);
        if (attribute.IsIndexed)
            _avetCurrent.Delete(batch, prevDatom.RawSpan);

        _txLog.Put(batch, datom);
        if (attribute.NoHistory) return;

        // Move the datom to the history index and also record the retraction
        _eavtHistory.Put(batch, prevDatom.RawSpan);
        _eavtHistory.Put(batch, datom);

        // Move the datom to the history index and also record the retraction
        _aevtHistory.Put(batch, prevDatom.RawSpan);
        _aevtHistory.Put(batch, datom);

        if (attribute.IsReference)
        {
            _vaetHistory.Put(batch, prevDatom.RawSpan);
            _vaetHistory.Put(batch, datom);
        }

        if (attribute.IsIndexed)
        {
            _avetHistory.Put(batch, prevDatom.RawSpan);
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
            var found = snapshot.Datoms(IndexType.EAVTCurrent, span)
                .Select(d => d.Clone())
                .FirstOrDefault();
            if (!found.Valid) return PrevState.NotExists;
            if (found.E != keyPrefix.E || found.A != keyPrefix.A)
                return PrevState.NotExists;

            var aSpan = found.ValueSpan;
            var bSpan = span.SliceFast(sizeof(KeyPrefix));
            fixed (byte* a = aSpan)
            fixed (byte* b = bSpan)
            {
                var cmp = ValueComparer.CompareValues(a, aSpan.Length, b, bSpan.Length);
                return cmp == 0 ? PrevState.Duplicate : PrevState.NotExists;
            }
        }
        else
        {
            KeyPrefix start = default;
            start.Set(keyPrefix.E, keyPrefix.A, TxId.MinValue, false);

            var datom = snapshot.Datoms(IndexType.EAVTCurrent, start)
                .Select(d => d.Clone())
                .FirstOrDefault();
            if (!datom.Valid) return PrevState.NotExists;

            var currKey = datom.Prefix;
            if (currKey.E != keyPrefix.E || currKey.A != keyPrefix.A)
                return PrevState.NotExists;

            var aSpan = datom.ValueSpan;
            var bSpan = span.SliceFast(sizeof(KeyPrefix));
            fixed (byte* a = aSpan)
            fixed (byte* b = bSpan)
            {
                var cmp = ValueComparer.CompareValues(a, aSpan.Length, b, bSpan.Length);
                if (cmp == 0) return PrevState.Duplicate;
            }

            _retractWriter.Reset();
            _retractWriter.Write(datom.RawSpan);

            return PrevState.Exists;
        }

    }


    #endregion
}
