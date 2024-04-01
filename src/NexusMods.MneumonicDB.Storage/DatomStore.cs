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
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Abstractions.Internals;
using NexusMods.MneumonicDB.Storage.Abstractions;
using NexusMods.MneumonicDB.Storage.DatomStorageStructures;
using Reloaded.Memory.Extensions;

namespace NexusMods.MneumonicDB.Storage;

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
    private readonly PooledMemoryBufferWriter _prevWriter;
    private readonly AttributeRegistry _registry;
    private readonly DatomStoreSettings _settings;
    private readonly Channel<PendingTransaction> _txChannel;
    private readonly IIndex _txLog;
    private readonly Subject<(TxId TxId, ISnapshot snapshot)> _updatesSubject;
    private readonly IIndex _vaetCurrent;
    private readonly IIndex _vaetHistory;
    private readonly PooledMemoryBufferWriter _writer;

    private TxId _asOfTxId = TxId.MinValue;
    private EntityId _nextEntityId;


    public DatomStore(ILogger<DatomStore> logger, AttributeRegistry registry, DatomStoreSettings settings,
        IStoreBackend backend)
    {
        _backend = backend;


        _writer = new PooledMemoryBufferWriter();
        _prevWriter = new PooledMemoryBufferWriter();


        _logger = logger;
        _settings = settings;
        _registry = registry;
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
        var _ = Bootstrap();
        Task.Run(ConsumeTransactions);
    }

    public TxId AsOfTxId => _asOfTxId;
    public IAttributeRegistry Registry => _registry;


    public async Task<TxId> Sync()
    {
        await Transact(Enumerable.Empty<IWriteDatom>());
        return _asOfTxId;
    }

    public async Task<StoreResult> Transact(IEnumerable<IWriteDatom> datoms)
    {
        var pending = new PendingTransaction { Data = datoms.ToArray() };
        if (!_txChannel.Writer.TryWrite(pending))
            throw new InvalidOperationException("Failed to write to the transaction channel");

        return await pending.CompletionSource.Task;
    }

    public IObservable<(TxId TxId, ISnapshot Snapshot)> TxLog => _updatesSubject;

    public async Task RegisterAttributes(IEnumerable<DbAttribute> newAttrs)
    {
        var datoms = new List<IWriteDatom>();
        var newAttrsArray = newAttrs.ToArray();

        foreach (var attr in newAttrsArray)
        {
            datoms.Add(BuiltInAttributes.UniqueId.Assert(EntityId.From(attr.AttrEntityId.Value), attr.UniqueId));
            datoms.Add(BuiltInAttributes.ValueSerializerId.Assert(EntityId.From(attr.AttrEntityId.Value),
                attr.ValueTypeId));
        }

        await Transact(datoms);

        _registry.Populate(newAttrsArray);
    }

    public ISnapshot GetSnapshot()
    {
        return _backend.GetSnapshot();
    }

    public void Dispose()
    {
        _updatesSubject.Dispose();
        _writer.Dispose();
        _prevWriter.Dispose();
    }

    private async Task ConsumeTransactions()
    {
        var sw = Stopwatch.StartNew();
        while (await _txChannel.Reader.WaitToReadAsync())
        {
            var pendingTransaction = await _txChannel.Reader.ReadAsync();
            try
            {
                // Sync transactions have no data, and are used to verify that the store is up to date.
                if (pendingTransaction.Data.Length == 0)
                {
                    var storeResult = new StoreResult
                    {
                        Remaps = new Dictionary<EntityId, EntityId>(),
                        AssignedTxId = _asOfTxId,
                        Snapshot = _backend.GetSnapshot(),
                    };
                    pendingTransaction.CompletionSource.SetResult(storeResult);
                    continue;
                }

                Log(pendingTransaction, out var result);

                _updatesSubject.OnNext((result.AssignedTxId, result.Snapshot));
                pendingTransaction.CompletionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                pendingTransaction.CompletionSource.TrySetException(ex);
            }
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
            using var txIterator = snapshot.GetIterator(IndexType.TxLog);
            var lastTx = txIterator
                .SeekLast()
                .Reverse()
                .Resolve()
                .FirstOrDefault()?.T ?? TxId.MinValue;

            if (lastTx == TxId.MinValue)
            {
                _logger.LogInformation("Bootstrapping the datom store no existing state found");
                var _ = await Transact(BuiltInAttributes.InitialDatoms);
                return;
            }

            _logger.LogInformation("Bootstrapping the datom store, existing state found, last tx: {LastTx}",
                lastTx.Value.ToString("x"));
            _asOfTxId = lastTx;

            using var entIterator = snapshot.GetIterator(IndexType.EAVTCurrent);
            var lastEnt = entIterator
                .SeekLast()
                .Reverse()
                .Resolve()
                .FirstOrDefault()?.E ?? EntityId.MinValue;

            _nextEntityId = EntityId.From(lastEnt.Value + 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bootstrap the datom store");
        }
    }

    public Expression GetValueReadExpression(Type attribute, Expression valueSpan, out AttributeId attributeId)
    {
        return _registry.GetReadExpression(attribute, valueSpan, out attributeId);
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


    private unsafe void Log(PendingTransaction pendingTransaction, out StoreResult result)
    {
        var thisTx = TxId.From(_asOfTxId.Value + 1);


        var remaps = new Dictionary<EntityId, EntityId>();
        var remapFn = (Func<EntityId, EntityId>)(id => MaybeRemap(id, remaps, thisTx));
        using var batch = _backend.CreateBatch();

        var swPrepare = Stopwatch.StartNew();
        foreach (var datom in pendingTransaction.Data)
        {
            _writer.Reset();
            _writer.Advance(sizeof(KeyPrefix));

            var isRemapped = Ids.IsPartition(datom.E.Value, Ids.Partition.Tmp);
            datom.Explode(_registry, remapFn, out var e, out var a, _writer, out var isRetract);
            var keyPrefix = _writer.GetWrittenSpanWritable().CastFast<byte, KeyPrefix>();
            keyPrefix[0].Set(e, a, thisTx, isRetract);

            var attr = _registry.GetAttribute(a);
            var isReference = attr.IsReference;
            var isIndexed = attr.IsIndexed;

            var havePrevious = false;
            if (!isRemapped)
                havePrevious = GetPrevious(keyPrefix[0]);

            // Put it in the tx log first
            var newSpan = _writer.GetWrittenSpan();
            _txLog.Put(batch, newSpan);

            // Remove the previous if it exists
            if (havePrevious)
            {
                var prevValSpan = _prevWriter.GetWrittenSpan().SliceFast(sizeof(KeyPrefix));
                var currValSpan = _writer.GetWrittenSpan().SliceFast(sizeof(KeyPrefix));

                if (!isRetract && prevValSpan.SequenceEqual(currValSpan))
                    continue;

                // Move the previous to the history index
                var span = _prevWriter.GetWrittenSpan();
                _eavtCurrent.Delete(batch, span);
                _eavtHistory.Put(batch, span);

                _aevtCurrent.Delete(batch, span);
                _aevtHistory.Put(batch, span);

                if (isReference)
                {
                    _vaetCurrent.Delete(batch, span);
                    _vaetHistory.Put(batch, span);
                }

                if (isIndexed)
                {
                    _avetCurrent.Delete(batch, span);
                    _avetHistory.Put(batch, span);
                }
            }


            // Add new state
            if (!isRetract)
            {
                _eavtCurrent.Put(batch, newSpan);
                _aevtCurrent.Put(batch, newSpan);

                if (isReference)
                    _vaetCurrent.Put(batch, newSpan);

                if (isIndexed)
                    _avetCurrent.Put(batch, newSpan);
            }
            else
            {
                _eavtHistory.Put(batch, newSpan);
                _aevtHistory.Put(batch, newSpan);

                if (isReference)
                    _vaetHistory.Put(batch, newSpan);

                if (isIndexed)
                    _aevtHistory.Put(batch, newSpan);
            }
        }

        var swWrite = Stopwatch.StartNew();
        batch.Commit();

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Transaction {TxId} ({Count} datoms) prepared in {Elapsed}ms, written in {WriteElapsed}ms",
                thisTx.Value,
                pendingTransaction.Data.Length,
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

    private void ProcessRetract(IWriteBatch batch, IAttribute attribute, ReadOnlySpan<byte> datom)
    {
        _eavtCurrent.Delete(batch, datom);
        _aevtCurrent.Delete(batch, datom);
        if (attribute.IsReference)
            _vaetCurrent.Delete(batch, datom);
        if (attribute.IsIndexed)
            _avetCurrent.Delete(batch, datom);

        if (attribute.NoHistory) return;

        _eavtHistory.Put(batch, datom);
        _aevtHistory.Put(batch, datom);

        if (attribute.IsReference)
            _vaetHistory.Delete(batch, datom);
        if (attribute.IsIndexed)
            _avetHistory.Delete(batch, datom);
    }

    private void ProcessAssert(IWriteBatch batch, IAttribute attribute, ReadOnlySpan<byte> datom)
    {
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

    private unsafe PrevState GetPreviousState(IAttribute attribute, IValueSerializer serializer, ISeekableIterator iterator, ReadOnlySpan<byte> span)
    {
        var keyPrefix = MemoryMarshal.Read<KeyPrefix>(span);

        if (attribute.IsMultiCardinality)
        {
            var iter = iterator.Seek(span);
            if (!iter.Valid) return PrevState.NotExists;
            var iterKey = iter.CurrentKeyPrefix();
            if (iterKey.E != keyPrefix.E || iterKey.A != keyPrefix.A)
                return PrevState.NotExists;

            if (serializer.Compare(iter.Current.SliceFast(sizeof(KeyPrefix)),
                    span.SliceFast(sizeof(KeyPrefix))) == 0)
                return PrevState.Duplicate;

            return PrevState.NotExists;
        }
        else
        {
            var iter = iterator.SeekTo(keyPrefix.E, keyPrefix.A);
            if (!iter.Valid) return PrevState.NotExists;

        }

    }

    private bool GetPrevious(KeyPrefix d)
    {
        var prefix = new KeyPrefix();
        prefix.Set(d.E, d.A, TxId.MinValue, false);
        using var source = _eavtCurrent.GetIterator();
        var iter = source.Seek(MemoryMarshal.CreateSpan(ref prefix, 1).Cast<KeyPrefix, byte>());
        if (!iter.Valid) return false;

        var found = MemoryMarshal.Read<KeyPrefix>(iter.Current);
        if (found.E != d.E || found.A != d.A) return false;

        _prevWriter.Reset();
        _prevWriter.Write(iter.Current);
        return true;
    }

    #endregion
}
