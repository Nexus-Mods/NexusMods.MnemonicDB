using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.LargeTestModel.Attributes;

/// <summary>
/// A Guid attribute stored as a UInt128
/// </summary>
public class GuidAttribute(string ns, string name) : ScalarAttribute<Guid, UInt128, UInt128Serializer>(ns, name)
{
    protected override UInt128 ToLowLevel(Guid value)
    {
        Span<byte> bytes = stackalloc byte[16];
        value.TryWriteBytes(bytes);
        return MemoryMarshal.Read<UInt128>(bytes);
    }

    protected override Guid FromLowLevel(UInt128 value, AttributeResolver resolver)
    {
        Span<byte> bytes = stackalloc byte[16];
        MemoryMarshal.Write(bytes, value);
        return new Guid(bytes);
    }
}
