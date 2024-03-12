using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Nodes.DataNode;

public class Appendable : IDataNode
{
    private Columns.ULongColumns.Appendable _entityIds;
    private Columns.ULongColumns.Appendable _attributeIds;
    private Columns.BlobColumns.Appendable _values;
    private Columns.ULongColumns.Appendable _transactionIds;
    private readonly int _length;

    public Appendable()
    {
        _length = 0;
        _entityIds = Columns.ULongColumns.Appendable.Create();
        _attributeIds = Columns.ULongColumns.Appendable.Create();
        _values = Columns.BlobColumns.Appendable.Create();
        _transactionIds = Columns.ULongColumns.Appendable.Create();
    }

    public IEnumerator<Datom> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public long DeepLength => _length;
    public int Length => _length;

    public Datom this[int idx] => new()
    {
        E = EntityId.From(_entityIds[idx]),
        A = AttributeId.From(_attributeIds[idx]),
        V = _values.GetMemory(idx),
        T = TxId.From(_transactionIds[idx])
    };

    public Datom LastDatom => this[_length - 1];
    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        throw new System.NotImplementedException();
    }

    public IDataNode Flush(INodeStore store)
    {
        throw new System.NotImplementedException();
    }

    public int FindEATV(int start, int end, in Datom target, IAttributeRegistry registry)
    {
        throw new System.NotImplementedException();
    }

    public int FindAVTE(int start, int end, in Datom target, IAttributeRegistry registry)
    {
        throw new System.NotImplementedException();
    }

    public int FindAETV(int start, int end, in Datom target, IAttributeRegistry registry)
    {
        throw new System.NotImplementedException();
    }

    public int Find(int start, int end, in Datom target, SortOrders order, IAttributeRegistry registry)
    {
        throw new System.NotImplementedException();
    }
}
