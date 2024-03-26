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
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.DatomStorageStructures;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage;


public class DatomStore : IDatomStore
{
    private readonly AttributeRegistry _registry;
    private readonly ILogger<DatomStore> _logger;
    private readonly Channel<PendingTransaction> _txChannel;
    private EntityId _nextEntityId;
    private readonly Subject<(TxId TxId, IReadOnlyCollection<IReadDatom> Datoms)> _updatesSubject;
    private readonly DatomStoreSettings _settings;


    #region Indexes



    #endregion


    private TxId _asOfTxId = TxId.MinValue;
    private readonly PooledMemoryBufferWriter _writer;
    private readonly IStoreBackend _backend;
    private readonly IIndex _eavtHistory;
    private readonly IIndex _eavtCurrent;
    private readonly IIndex _aevtCurrent;
    private readonly IIndex _aevtHistory;
    private readonly IIndex _txLog;
    private readonly PooledMemoryBufferWriter _prevWriter;
    private readonly IIndex _vaetCurrent;
    private readonly IIndex _vaetHistory;
    private readonly IIndex _avetCurrent;
    private readonly IIndex _avetHistory;


    public DatomStore(ILogger<DatomStore> logger, AttributeRegistry registry, DatomStoreSettings settings, IStoreBackend backend)
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


        _updatesSubject = new Subject<(TxId TxId, IReadOnlyCollection<IReadDatom> Datoms)>();

        registry.Populate(BuiltInAttributes.Initial);

        _txChannel = Channel.CreateUnbounded<PendingTransaction>();
        var _ = Bootstrap();
        Task.Run(ConsumeTransactions);
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
                    var storeResult = new StoreResult()
                    {
                        Remaps = new Dictionary<EntityId, EntityId>(),
                        AssignedTxId = _asOfTxId,
                        Snapshot = _backend.GetSnapshot(),
                        Datoms = Array.Empty<IReadDatom>()
                    };
                    pendingTransaction.CompletionSource.SetResult(storeResult);
                    continue;
                }

                Log(pendingTransaction, out var result);

                //_updatesSubject.OnNext((_asOfTxId, readables));
                pendingTransaction.CompletionSource.SetResult(result);

                //_logger.LogDebug("Transaction {TxId} processed in {Elapsed}ms, new in-memory size is {Count} datoms", pendingTransaction.AssignedTxId!.Value, sw.ElapsedMilliseconds, _indexes.InMemorySize);
            }
            catch (Exception ex)
            {
                pendingTransaction.CompletionSource.TrySetException(ex);
            }
        }
    }

    /// <summary>
    /// Sets up the initial state of the store.
    /// </summary>
    private async Task Bootstrap()
    {
        try
        {
            //var lastTx = GetMostRecentTxId();
            var lastTx = TxId.MinValue;
            if (lastTx == TxId.MinValue)
            {
                _logger.LogInformation("Bootstrapping the datom store no existing state found");
                var _ = await Transact(BuiltInAttributes.InitialDatoms);
                return;
            }
            else
            {
                _logger.LogInformation("Bootstrapping the datom store, existing state found, last tx: {LastTx}", lastTx.Value.ToString("x"));
                _asOfTxId = lastTx;
            }

            _nextEntityId = EntityId.From(GetMaxEntityId().Value + 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bootstrap the datom store");
        }
    }

    public TxId AsOfTxId => _asOfTxId;
    public IAttributeRegistry Registry => _registry;

    public void Dispose()
    {
        /*_txChannel.Writer.Complete();

        _db.Dispose();
        _txLog.Dispose();
        _eatvCurrent.Dispose();
        _eatvHistory.Dispose();
        _aetvCurrent.Dispose();
        _backrefHistory.Dispose();*/
        throw new NotImplementedException();
    }

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

    public IObservable<(TxId TxId, IReadOnlyCollection<IReadDatom> Datoms)> TxLog => _updatesSubject;

    public IEnumerable<IReadDatom> Resolved(IEnumerable<Datom> datoms)
    {
        return datoms.Select(datom => _registry.Resolve(datom));
    }

    public async Task RegisterAttributes(IEnumerable<DbAttribute> newAttrs)
    {
        var datoms = new List<IWriteDatom>();
        var newAttrsArray = newAttrs.ToArray();

        foreach (var attr in newAttrsArray)
        {
            datoms.Add(BuiltInAttributes.UniqueId.Assert(EntityId.From(attr.AttrEntityId.Value), attr.UniqueId));
            datoms.Add(BuiltInAttributes.ValueSerializerId.Assert(EntityId.From(attr.AttrEntityId.Value), attr.ValueTypeId));
        }

        await Transact(datoms);

        _registry.Populate(newAttrsArray);
    }

    public Expression GetValueReadExpression(Type attribute, Expression valueSpan, out AttributeId attributeId)
    {
        return _registry.GetReadExpression(attribute, valueSpan, out attributeId);
    }

    public IEnumerable<EntityId> GetReferencesToEntityThroughAttribute<TAttribute>(EntityId id, TxId txId)
        where TAttribute : IAttribute<EntityId>
    {
//           return _backrefHistory.GetReferencesToEntityThroughAttribute<TAttribute>(id, txId);
throw new NotImplementedException();
    }


    public bool TryGetExact<TAttr, TValue>(EntityId e, TxId tx, out TValue val) where TAttr : IAttribute<TValue>
    {
        /*if (_eatvHistory.TryGetExact<TAttr, TValue>(e, tx, out var foundVal))
        {
            val = foundVal;
            return true;
        }
        val = default!;
        return false;*/
        throw new NotImplementedException();
    }

    public bool TryGetLatest<TAttribute, TValue>(EntityId e, TxId tx, out TValue value)
        where TAttribute : IAttribute<TValue>
    {
        /*
        if (_eatvCurrent.TryGet<TAttribute, TValue>(e, tx, out var foundVal) == LookupResult.Found)
        {
            value = foundVal;
            return true;
        }

        if (_eatvHistory.TryGetLatest<TAttribute, TValue>(e, tx, out foundVal))
        {
            value = foundVal;
            return true;
        }

        value = default!;
        return false;
        */
        throw new NotImplementedException();
    }

    public IEnumerable<EntityId> GetEntitiesWithAttribute<TAttribute>(TxId txId)
        where TAttribute : IAttribute
    {

        throw new NotImplementedException();
    }

    public IEnumerable<IReadDatom> GetAttributesForEntity(EntityId entityId, TxId txId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the maximum entity id in the store.
    /// </summary>
    public EntityId GetMaxEntityId()
    {
        /*
        return _eatvCurrent.GetMaxEntityId();
        */
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the most recent transaction id.
    /// </summary>
    public TxId GetMostRecentTxId()
    {
        /*
        return _txLog.GetMostRecentTxId();
        */
        throw new NotImplementedException();
    }

    public Type GetReadDatomType(Type attribute)
    {
        return _registry.GetReadDatomType(attribute);
    }

    public ISnapshot GetSnapshot()
    {
        return _backend.GetSnapshot();
    }

    public IEnumerable<IReadDatom> SeekIndex(ISnapshot snapshot, IndexType type, EntityId? entityId = default, AttributeId? attributeId = default, TxId? txId = default)
    {
        using var iter = snapshot.GetIterator(type);
        Span<byte> datom = stackalloc byte[KeyPrefix.Size];

        var prefix = datom.CastFast<byte, KeyPrefix>();
        prefix[0].Set(entityId ?? EntityId.From(0), attributeId ?? AttributeId.From(0), txId ?? TxId.MinValue, false);
        iter.Seek(datom);

        while (iter.Valid)
        {
            var c = MemoryMarshal.Read<KeyPrefix>(iter.Current.SliceFast(0, KeyPrefix.Size));
            var resolved = _registry.Resolve(c.E, c.A, iter.Current.SliceFast(KeyPrefix.Size), c.T, c.IsRetract);
            yield return resolved;
            iter.Next();
        }

    }

    public IEnumerable<IReadDatom> Datoms(ISnapshot snapshot, IndexType type)
    {
        using var iter = snapshot.GetIterator(type);
        iter.SeekStart();
        while (iter.Valid)
        {
            var c = MemoryMarshal.Read<KeyPrefix>(iter.Current.SliceFast(0, KeyPrefix.Size));
            var resolved = _registry.Resolve(c.E, c.A, iter.Current.SliceFast(KeyPrefix.Size), c.T, c.IsRetract);
            yield return resolved;
            iter.Next();

        }
    }

    #region Internals


    EntityId MaybeRemap(EntityId id, Dictionary<EntityId, EntityId> remaps, TxId thisTx)
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
            else
            {
                return newId;
            }
        }
        return id;
    }



    private void Log(PendingTransaction pendingTransaction, out StoreResult result)
    {

        var output = new List<IReadDatom>();

        var thisTx = TxId.From(_asOfTxId.Value + 1);


        var remaps = new Dictionary<EntityId, EntityId>();
        var remapFn = (Func<EntityId, EntityId>)(id => MaybeRemap(id, remaps, thisTx));
        using var batch = _backend.CreateBatch();

        var swPrepare = Stopwatch.StartNew();
        foreach (var datom in pendingTransaction.Data)
        {
            _writer.Reset();
            unsafe
            {
                _writer.Advance(sizeof(KeyPrefix));
            }

            var isRemapped = Ids.IsPartition(datom.E.Value, Ids.Partition.Tmp);
            datom.Explode(_registry, remapFn, out var e, out var a, _writer, out var isAssert);
            var keyPrefix = _writer.GetWrittenSpanWritable().CastFast<byte, KeyPrefix>();
            keyPrefix[0].Set(e, a, thisTx, isAssert);

            var attr = _registry.GetAttribute(a);
            var isReference = attr.IsReference;
            var isIndexed = attr.IsIndexed;

            var havePrevious = false;
            if (!isRemapped)
                havePrevious = GetPrevious(keyPrefix[0]);

            if (havePrevious)
            {
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

            var newSpan = _writer.GetWrittenSpan();
            _eavtCurrent.Put(batch, newSpan);
            _aevtCurrent.Put(batch, newSpan);
            _txLog.Put(batch, newSpan);

            if (isReference)
                _vaetCurrent.Put(batch, newSpan);

            if (isIndexed)
                _avetCurrent.Put(batch, newSpan);


            //output.Add(_registry.Resolve(EntityId.From(stackDatom.E), AttributeId.From(stackDatom.A), stackDatom.V, TxId.From(stackDatom.T)));
        }

        var swWrite = Stopwatch.StartNew();
        batch.Commit();

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Transaction {TxId} ({Count} datoms) prepared in {Elapsed}ms, written in {WriteElapsed}ms",
                thisTx.Value,
                pendingTransaction.Data.Length,
                swPrepare.ElapsedMilliseconds - swWrite.ElapsedMilliseconds,
                swWrite.ElapsedMilliseconds);
        }


        result = new StoreResult
        {
            AssignedTxId = thisTx,
            Remaps = remaps,
            Datoms = output,
            Snapshot = _backend.GetSnapshot()
        };


        _asOfTxId = thisTx;
    }

    private bool GetPrevious(KeyPrefix d)
    {
        var prefix = new KeyPrefix();
        prefix.Set(d.E, d.A, TxId.MinValue, false);
        var iter = _eavtCurrent.GetIterator();
        iter.Seek(MemoryMarshal.CreateSpan(ref prefix, 1).Cast<KeyPrefix, byte>());
        if (iter.Valid)
        {
            var found = MemoryMarshal.Read<KeyPrefix>(iter.Current);
            if (found.E == d.E && found.A == d.A)
            {
                _prevWriter.Reset();
                _prevWriter.Write(iter.Current);
                return true;
            }

        }

        return false;
    }

    #endregion
}
