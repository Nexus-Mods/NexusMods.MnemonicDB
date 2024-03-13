using System;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;
using NexusMods.EventSourcing.Abstractions.Nodes.DataNode;

namespace NexusMods.EventSourcing.Storage.Nodes.DataNode;

public abstract class AReadable : IReadable
{
    protected int _length = 0;
    public int Length => _length;

    public abstract EntityId GetEntityId(int idx);
    public abstract AttributeId GetAttributeId(int idx);
    public abstract TxId GetTransactionId(int idx);
    public abstract ReadOnlySpan<byte> GetValue(int idx);

    public abstract int FillChunk(int offset, int length, ref DatomChunk chunk);
}
