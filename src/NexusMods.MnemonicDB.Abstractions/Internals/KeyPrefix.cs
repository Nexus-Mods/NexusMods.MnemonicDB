using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.Internals;

/// <summary>
/// Represents a key prefix used in the MnemonicDB system.
/// The KeyPrefix is a fixed-size structure (16 bytes) that encodes various parameters:
/// - Upper 8 bytes:
///   - Bits 48-63: AttributeId (16 bits)
///   - Bits 40-47: IndexType (8 bits)
///   - Bits 0-39: TxId (40 bits)
/// - Lower 8 bytes:
///   - Bits 56-63: EntityId (8 bits)
///   - Bits 8-55: EntityId (48 bits)
///   - Bit 0: IsRetract (1 bit)
///   - Bits 1-7: ValueTag (7 bits)
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
    public KeyPrefix(EntityId id, AttributeId attributeId, TxId txId, bool isRetract, ValueTag tags, IndexType index = IndexType.None)
    {
        _upper = ((ulong)attributeId << 48) | ((ulong)txId & 0x000000FFFFFFFFFF) | ((ulong)index << 40);
        _lower = ((ulong)id & 0xFF00000000000000) | (((ulong)id & 0x0000FFFFFFFFFFFF) << 8) | (isRetract ? 1UL : 0UL) | ((ulong)tags << 1);
    }

    /// <summary>
    /// The index this datom is stored in. This will be 0 (None)
    /// for most datoms seen by the user, but the backend datomstore
    /// will use this field for the various sorted indexes.
    /// </summary>
    public IndexType Index
    {
        get => (IndexType)((_upper >> 40) & 0xFF);
        init => _upper = (_upper & 0xFFFF00FFFFFFFFFF) | ((ulong)value << 40);
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
        init => _lower = (_lower & 0xFFFFFFFFFFFFFF01) | ((ulong)value << 1);
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
            var id = PartitionId.Transactions.MakeEntityId(_upper & 0x000000FFFFFFFFFF).Value;
            return Unsafe.As<ulong, TxId>(ref id);
        }
        init => _upper = (_upper & 0xFFFFFF0000000000) | ((ulong)value & 0x000000FFFFFFFFFF);
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
