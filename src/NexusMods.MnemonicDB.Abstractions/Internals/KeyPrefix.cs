using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.Internals;

/// <summary>
///     The system encodes keys as a 16 byte prefix followed by the actual key data, the format
///     of the value is defined by the IValueSerializer
///         and the length is maintained by the datastore.
///         This KeyPrefix then contains the other parts of the Datom: EntityId, AttributeId, TxId, and Flags.
///         Encodes and decodes the prefix of a key, the format is:
///         [AttributeId: 2bytes]
///         [TxId: 6bytes]
///         [EntityID + PartitionID: 7bytes]
///         [IsRetract: 1 bit] [ValueTag: 7 bits]
///         The Entity Id is created by taking the last 6 bytes of the id and combining it with
///         the partition id. So the encoding logic looks like this:
///         packed = (e & 0x00FFFFFFFFFFFFFF) >> 8 | (e & 0xFFFFFFFFFFFF) << 8
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = Size)]
public readonly record struct KeyPrefix
{
    /// <summary>
    ///     Fixed size of the KeyPrefix
    /// </summary>
    public const int Size = 16;

    [FieldOffset(0)] private readonly ulong _upper;
    [FieldOffset(8)] private readonly ulong _lower;

    /// <summary>
    /// The upper 8 bytes of the key
    /// </summary>
    public ulong Upper => _upper;

    /// <summary>
    /// The lower 8 bytes of the key
    /// </summary>
    public ulong Lower => _lower;

    /// <summary>
    ///    Sets the key prefix to the given values
    /// </summary>
    public KeyPrefix(ulong upper, ulong lower)
    {
        _upper = upper;
        _lower = lower;
    }

    /// <summary>
    /// Disallow the default constructor
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public KeyPrefix()
    {
        throw new InvalidOperationException("This constructor should not be called, use the Create method instead");
    }

    /// <summary>
    /// Creates a new KeyPrefix with the given values
    /// </summary>
    public KeyPrefix(EntityId id, AttributeId attributeId, TxId txId, bool isRetract, ValueTag tags)
    {
        _upper = ((ulong)attributeId << 48) | ((ulong)txId & 0x0000FFFFFFFFFFFF);
        _lower = ((ulong)id & 0xFF00000000000000) | (((ulong)id & 0x0000FFFFFFFFFFFF) << 8) | (isRetract ? 1UL : 0UL) | ((ulong)tags << 1);
    }

    /// <summary>
    ///     The EntityId
    /// </summary>
    public EntityId E
    {
        get
        {
            var val = _lower & 0xFF00000000000000 | ((_lower >> 8) & 0x0000FFFFFFFFFFFF);
            return Unsafe.As<ulong, EntityId>(ref val);
        }
        init => _lower = (_lower & 0xFF) | ((ulong)value & 0xFF00000000000000) | (((ulong)value & 0x0000FFFFFFFFFFFF) << 8);
    }
    
    /// <summary>
    ///     True if this is a retraction
    /// </summary>
    public bool IsRetract
    {
        get => (_lower & 1) == 1;
        init => _lower = value ? _lower | 1UL : _lower & ~1UL;
    }

    /// <summary>
    ///  The value tag of the datom, which defines the actual value type of the datom
    /// </summary>
    public ValueTag ValueTag
    {
        get => (ValueTag)((_lower >> 1) & 0x7F);
        init => _lower = (_lower & 0xFF00000000000001) | ((ulong)value << 1);
    }

    /// <summary>
    ///     The attribute id, maximum of 2^16 attributes are supported in the system
    /// </summary>
    public AttributeId A
    {
        get
        {
            var val = (ushort)(_upper >> 48);
            return Unsafe.As<ushort, AttributeId>(ref val);
        }
        init => _upper = (_upper & 0x0000FFFFFFFFFFFF) | ((ulong)value << 48);
    }

    /// <summary>
    ///     The transaction id, maximum of 2^63 transactions are supported in the system, but really
    ///     it's 2^56 as the upper 8 bits are used for the partition id.
    /// </summary>
    public TxId T
    {
        get
        {
            var id = PartitionId.Transactions.MakeEntityId(_upper & 0x0000FFFFFFFFFFFF).Value;
            return Unsafe.As<ulong, TxId>(ref id);
        }
        init => _upper = (_upper & 0xFFFF000000000000) | ((ulong)value & 0x0000FFFFFFFFFFFF);
    }

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

    #region Constants

    public static KeyPrefix Min => new(0, 0);

    public static KeyPrefix Max => new(0, 0);

    /// <summary>
    /// Returns true if this key is valid
    /// </summary>
    public bool IsValid => _upper != 0;

    #endregion
}
