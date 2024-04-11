using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Storage.Serializers;

internal class TxIdSerializer : IValueSerializer<TxId>
{
    public Type NativeType => typeof(TxId);
    public LowLevelTypes LowLevelType { get; }
    public Symbol UniqueId { get; } = Symbol.Intern<TxIdSerializer>();


    public TxId Read(in KeyPrefix prefix, ReadOnlySpan<byte> valueSpan)
    {
        Debug.Assert(prefix is { LowLevelType: LowLevelTypes.UInt, ValueLength: sizeof(ulong) });
        return TxId.From(MemoryMarshal.Read<ulong>(valueSpan));
    }

    public void Serialize<TWriter>(ref KeyPrefix prefix, TxId value, TWriter buffer) where TWriter : IBufferWriter<byte>
    {
        prefix.ValueLength = sizeof(ulong);
        prefix.LowLevelType = LowLevelTypes.UInt;
        var span = buffer.GetSpan(KeyPrefix.Size + sizeof(ulong));
        MemoryMarshal.Write(span, prefix);
        MemoryMarshal.Write(span.SliceFast(KeyPrefix.Size), value);
        buffer.Advance(KeyPrefix.Size + sizeof(ulong));
    }
}
