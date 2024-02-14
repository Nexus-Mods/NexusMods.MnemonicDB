using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NexusMods.EventSourcing.DatomStore;

[StructLayout(LayoutKind.Sequential, Size = 18)]
public unsafe struct KeyHeader
{
    public const int Size = 18;
    // The TX value
    private ulong _tx;

    // The EntityId
    private ulong _entity;

    // Highest bit is the "assert" flag, rest are the attribute id truncated to 15 bits
    private ushort _attrAndExtra;

    public ulong Tx
    {
        get => _tx;
        set => _tx = value;
    }

    public ulong Entity
    {
        get => _entity;
        set => _entity = value;
    }

    public ulong AttributeId
    {
        get => (ulong)(_attrAndExtra & 0x7FFF);
        set => _attrAndExtra = (ushort)((uint)(_attrAndExtra & 0x8000) | (value & 0x7FFF));
    }

    public bool IsAssert
    {
        get => (_attrAndExtra & 0x8000) != 0;
        set => _attrAndExtra = (ushort)((value ? 0x8000 : 0) | (_attrAndExtra & 0x7FFF));
    }

    public bool IsRetraction
    {
        get => !IsAssert;
        set => IsAssert = !value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CompareEntity(KeyHeader* a, KeyHeader* b)
    {
        if (a->Entity < b->Entity) return -1;
        return a->Entity > b->Entity ? 1 : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CompareAttribute(KeyHeader* a, KeyHeader* b)
    {
        if (a->AttributeId < b->AttributeId) return -1;
        return a->AttributeId > b->AttributeId ? 1 : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CompareTx(KeyHeader* a, KeyHeader* b)
    {
        if (a->Tx < b->Tx) return -1;
        return a->Tx > b->Tx ? 1 : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CompareIsAssert(KeyHeader* a, KeyHeader* b)
    {
        if (a->IsAssert && b->IsAssert) return 0;
        if (a->IsAssert) return 1;
        return -1;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CompareValues(AttributeRegistry registry, KeyHeader* a, uint aLength, KeyHeader* b, uint bLength)
    {
        if (a->AttributeId < b->AttributeId) return 1;
        if (a->AttributeId > b->AttributeId) return -1;

        var aVal = (byte*) a + Size;
        var bVal = (byte*) b + Size;

        return registry.CompareValues(a->AttributeId, aVal, aLength - Size, bVal, bLength - Size);
    }
}
