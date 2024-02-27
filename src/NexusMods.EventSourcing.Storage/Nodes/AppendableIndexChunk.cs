using System;
using System.Buffers;
using System.Collections.Generic;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions.Columns;
using NexusMods.EventSourcing.Storage.Columns;

namespace NexusMods.EventSourcing.Storage.Nodes;

public class AppendableIndexChunk : IIndexChunk
{
    private readonly UnsignedIntegerColumn<EntityId> _entityIds;
    private readonly UnsignedIntegerColumn<AttributeId> _attributeIds;
    private readonly UnsignedIntegerColumn<TxId> _transactionIds;
    private readonly UnsignedIntegerColumn<DatomFlags> _flags;
    private readonly AppendableBlobColumn _values;

    private List<IDataChunk> _children;

    public AppendableIndexChunk()
    {
        _children = new List<IDataChunk> { new AppendableChunk() };
        _entityIds = new UnsignedIntegerColumn<EntityId>();
        _attributeIds = new UnsignedIntegerColumn<AttributeId>();
        _transactionIds = new UnsignedIntegerColumn<TxId>();
        _flags = new UnsignedIntegerColumn<DatomFlags>();
        _values = new AppendableBlobColumn();

    }


    public int Length => _entityIds.Length;
    public IColumn<EntityId> EntityIds => _entityIds;
    public IColumn<AttributeId> AttributeIds => _attributeIds;
    public IColumn<TxId> TransactionIds => _transactionIds;
    public IColumn<DatomFlags> Flags => _flags;
    public IBlobColumn Values => _values;

    public Datom this[int idx]
    {
        get
        {
            var acc = 0;
            foreach (var child in _children)
            {
                if (idx < acc + child.Length)
                {
                    return child[idx - acc];
                }
                acc += child.Length;
            }
            throw new IndexOutOfRangeException();
        }
    }

    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<IDataChunk> Children => _children;
}
