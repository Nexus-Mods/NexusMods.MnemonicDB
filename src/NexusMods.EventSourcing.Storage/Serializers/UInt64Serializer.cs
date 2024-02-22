using NexusMods.EventSourcing.Storage.Interfaces;

namespace NexusMods.EventSourcing.Storage.Serializers;

public class UInt64Serializer : IValueSerializer<ulong>
{
    public uint Serialize<TWriter>(in TWriter writer, in ulong value, out ulong inlineValue)
    {
        inlineValue = value;
        return sizeof(ulong);
    }
}
