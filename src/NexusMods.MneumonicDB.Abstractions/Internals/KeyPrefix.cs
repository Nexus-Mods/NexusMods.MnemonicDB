using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NexusMods.MneumonicDB.Abstractions.Internals;

/// <summary>
///     The system encodes keys as a 16 byte prefix followed by the actual key data, the format
///     of the value is defined by the IValueSerializer
///     <T>
///         and the length is maintained by the datastore.
///         This KeyPrefix then contains the other parts of the Datom: EntityId, AttributeId, TxId, and Flags.
///         Encodes and decodes the prefix of a key, the format is:
///         [AttributeId: 2bytes]
///         [TxId: 6bytes]
///         [EntityID + PartitionID: 7bytes]
///         [IsRetract: 1bit]
///         The Entity Id is created by taking the last 6 bytes of the id and combining it with
///         the partition id. So the encoding logic looks like this:
///         packed = (e & 0x00FFFFFFFFFFFFFF) >> 8 | (e & 0xFFFFFFFFFFFF) << 8
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = Size)]
public struct KeyPrefix
{
    /// <summary>
    ///     Fixed size of the KeyPrefix
    /// </summary>
    public const int Size = 16;

    [FieldOffset(0)] private ulong _upper;
    [FieldOffset(8)] private ulong _lower;

    public void Set(EntityId id, AttributeId attributeId, TxId txId, bool isRetract)
    {
        _upper = ((ulong)attributeId << 48) | ((ulong)txId & 0x0000FFFFFFFFFFFF);
        _lower = ((ulong)id & 0xFF00000000000000) | (((ulong)id & 0x0000FFFFFFFFFFFF) << 8) | (isRetract ? 1UL : 0UL);
    }

    /// <summary>
    ///     The EntityId
    /// </summary>
    public EntityId E => (EntityId)((_lower & 0xFF00000000000000) | ((_lower >> 8) & 0x0000FFFFFFFFFFFF));

    /// <summary>
    ///     True if this is a retraction
    /// </summary>
    public bool IsRetract => (_lower & 1) == 1;

    /// <summary>
    ///     The attribute id, maximum of 2^16 attributes are supported in the system
    /// </summary>
    public AttributeId A => (AttributeId)(_upper >> 48);

    /// <summary>
    ///     The transaction id, maximum of 2^63 transactions are supported in the system, but really
    ///     it's 2^56 as the upper 8 bits are used for the partition id.
    /// </summary>
    public TxId T => (TxId)Ids.MakeId(Ids.Partition.Tx, _upper & 0x0000FFFFFFFFFFFF);

    /// <inheritdoc />
    public override string ToString()
    {
        return $"E: {E}, A: {A}, T: {T}, Retract: {IsRetract}";
    }


    /// <summary>
    ///     Gets the KeyPrefix from the given bytes
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static KeyPrefix Read(ReadOnlySpan<byte> bytes)
    {
        return MemoryMarshal.Read<KeyPrefix>(bytes);
    }
}
