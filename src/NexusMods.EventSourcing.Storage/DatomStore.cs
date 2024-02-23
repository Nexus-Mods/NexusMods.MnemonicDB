using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Cathei.LinqGen;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Sorters;

namespace NexusMods.EventSourcing.Storage;

public class DatomStore : IDatomStore
{
    private static readonly UInt128 RootKey = "5C68CD24-A4BF-42EA-8892-6BF24956BE74".ToUInt128Guid();
    private readonly IKvStore _kvStore;
    private readonly AttributeRegistry _registry;
    private RootNode _rootNode = null!;
    private readonly PooledMemoryBufferWriter _pooledWriter;
    private ulong _txId;


    public DatomStore(IKvStore kvStore, AttributeRegistry registry)
    {
        _kvStore = kvStore;
        _registry = registry;
        _pooledWriter = new PooledMemoryBufferWriter();
        _txId = Ids.MinId(Ids.Partition.Tx);
        Bootstrap();
    }

    /// <summary>
    /// Sets up the initial state of the store.
    /// </summary>
    private void Bootstrap()
    {
        _rootNode = new RootNode(_registry, Configuration.Default);
        Transact(BuiltInAttributes.InitialDatoms);
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }

    private struct IngestSink(PooledMemoryBufferWriter writer, AttributeRegistry registry, IEnumerator<IDatom> src, ulong txId) : IEnumerator<IngestSink>, IDatomSink, IRawDatom
    {
        public void Datom<TAttr, TVal>(ulong e, TVal v, bool isAssert) where TAttr : IAttribute<TVal>
        {
            EntityId = e;
            AttributeId = (ushort)registry.GetAttributeId<TAttr>();

            var usesLiteral = registry.WriteValue(v, writer, out var literal);

            ValueLiteral = usesLiteral ? literal : 0;

            Flags = 0;
            if (isAssert) Flags |= DatomFlags.Added;
            if (usesLiteral) Flags |= DatomFlags.InlinedData;
        }

        public void Dispose()
        {
            writer.Reset();
        }

        public bool MoveNext()
        {
            writer.Reset();

            if (!src.MoveNext())
                return false;

            src.Current.Emit(ref this);
            return true;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public IngestSink Current => this;

        object IEnumerator.Current => Current;

        public ulong EntityId { get; set; }
        public ushort AttributeId { get; set; }
        public ulong TxId => txId;
        public DatomFlags Flags { get; set; }
        public ReadOnlySpan<byte> ValueSpan => writer.GetWrittenSpan();
        public ulong ValueLiteral { get; set; }
    }

    public TxId Transact(IEnumerable<IDatom> datoms)
    {
        lock(this)
        {
            _txId += 1;
            var sink = new IngestSink(_pooledWriter, _registry, datoms.GetEnumerator(), _txId);
            _rootNode.Ingest(_rootNode, sink);
            return TxId.From(_txId);
        }
    }

    public IIterator<IRawDatom> Where<TAttr>(TxId txId) where TAttr : IAttribute
    {
        throw new NotImplementedException();
    }

    public IEntityIterator EntityIterator(TxId txId)
    {
        throw new NotImplementedException();
    }

    public void RegisterAttributes(IEnumerable<DbAttribute> newAttrs)
    {
        throw new NotImplementedException();
    }

    public Expression GetValueReadExpression(Type attribute, Expression valueSpan, out ulong attributeId)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<EntityId> ReverseLookup<TAttribute>(TxId txId) where TAttribute : IAttribute<EntityId>
    {
        throw new NotImplementedException();
    }
}
