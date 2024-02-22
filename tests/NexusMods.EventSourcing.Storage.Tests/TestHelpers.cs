
using NexusMods.EventSourcing.Storage.Datoms;
using NexusMods.EventSourcing.Storage.Nodes;
using NexusMods.EventSourcing.Storage.Serializers;

namespace NexusMods.EventSourcing.Storage.Tests;

public class TestHelpers
{
    private static UInt64Serializer _ulongSerializer = new();

    public static OnHeapDatom Assert(ulong e, ushort a, ulong tx, ulong value)
    {
        return new OnHeapDatom
        {
            EntityId = e,
            AttributeId = a,
            TxId = tx,
            Flags = (byte)(DatomFlags.Added | DatomFlags.InlinedData),
            ValueLiteral = value,
            ValueData = []
        };
    }
}
