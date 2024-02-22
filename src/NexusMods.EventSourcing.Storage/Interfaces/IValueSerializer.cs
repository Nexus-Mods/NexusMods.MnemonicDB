namespace NexusMods.EventSourcing.Storage.Interfaces;

public interface IValueSerializer<TValue>
{
    public uint Serialize<TWriter>(in TWriter writer, in TValue value, out ulong inlineValue);
}

