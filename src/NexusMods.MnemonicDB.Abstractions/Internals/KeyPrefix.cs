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
public struct KeyPrefix
{
    /// <summary>
    ///     Fixed size of the KeyPrefix
    /// </summary>
    public const int Size = 16;

    [FieldOffset(0)] public ulong Upper;
    [FieldOffset(8)] public ulong Lower;
    
    
    /// <summary>
    ///    Sets the key prefix to the given values
    /// </summary>
    public KeyPrefix(ulong upper, ulong lower)
    {
        Upper = upper;
        Lower = lower;
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
    /// A KeyPrefix pointing to the start of an index
    /// </summary>
    public KeyPrefix(IndexType index)
        : this(EntityId.MinValueNoPartition, AttributeId.Min, TxId.MinValue, false, ValueTag.Null, index)
    {}

    /// <summary>
    /// Creates a new KeyPrefix with the given values
    /// </summary>
    public KeyPrefix(EntityId id, AttributeId attributeId, TxId txId, bool isRetract, ValueTag tags, IndexType index = IndexType.None)
    {
        Upper = ((ulong)attributeId << 48) | ((ulong)txId & 0x000000FFFFFFFFFF) | ((ulong)index << 40);
        Lower = ((ulong)id & 0xFF00000000000000) | (((ulong)id & 0x0000FFFFFFFFFFFF) << 8) | (isRetract ? 1UL : 0UL) | ((ulong)tags << 1);
    }

    /// <summary>
    /// The index this datom is stored in. This will be 0 (None)
    /// for most datoms seen by the user, but the backend datomstore
    /// will use this field for the various sorted indexes.
    /// </summary>
    public IndexType Index
    {
        get => (IndexType)((Upper >> 40) & 0xFF);
        set => Upper = (Upper & 0xFFFF00FFFFFFFFFF) | ((ulong)value << 40);
    }
    

    /// <summary>
    ///     The EntityId
    /// </summary>
    public EntityId E
    {
        get
        {
            var val = Lower & 0xFF00000000000000 | ((Lower >> 8) & 0x0000FFFFFFFFFFFF);
            return Unsafe.As<ulong, EntityId>(ref val);
        }
        set => Lower = (Lower & 0xFF) | ((ulong)value & 0xFF00000000000000) | (((ulong)value & 0x0000FFFFFFFFFFFF) << 8);
    }
    
    /// <summary>
    ///     True if this is a retraction
    /// </summary>
    public bool IsRetract
    {
        get => (Lower & 1) == 1;
        set => Lower = value ? Lower | 1UL : Lower & ~1UL;
    }

    /// <summary>
    ///  The value tag of the datom, which defines the actual value type of the datom
    /// </summary>
    public ValueTag ValueTag
    {
        get => (ValueTag)((Lower >> 1) & 0x7F);
        set => Lower = (Lower & 0xFFFFFFFFFFFFFF01) | ((ulong)value << 1);
    }

    /// <summary>
    ///     The attribute id, maximum of 2^16 attributes are supported in the system
    /// </summary>
    public AttributeId A
    {
        get
        {
            var val = (ushort)(Upper >> 48);
            return Unsafe.As<ushort, AttributeId>(ref val);
        }
        set => Upper = (Upper & 0x0000FFFFFFFFFFFF) | ((ulong)value << 48);
    }

    /// <summary>
    ///     The transaction id, maximum of 2^63 transactions are supported in the system, but really
    ///     it's 2^56 as the upper 8 bits are used for the partition id.
    /// </summary>
    public TxId T
    {
        get
        {
            var id = PartitionId.Transactions.MakeEntityId(Upper & 0x000000FFFFFFFFFF).Value;
            return Unsafe.As<ulong, TxId>(ref id);
        }
        set => Upper = (Upper & 0xFFFFFF0000000000) | ((ulong)value & 0x000000FFFFFFFFFF);
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

    /// <summary>
    /// The minimum key prefix possible
    /// </summary>
    public static readonly KeyPrefix Min = new(0, 0) { Index = IndexType.TxLog };

    /// <summary>
    /// The maximum key prefix possible
    /// </summary>
    public static readonly KeyPrefix Max = new(ulong.MaxValue, ulong.MaxValue) { Index = IndexType.AVETHistory };

    /// <summary>
    /// Returns true if this key is valid
    /// </summary>
    public bool IsValid => Upper != 0;
    
    /// <summary>
    /// The largest possible TxId that can roundtrip through the KeyPrefix. With all the bitbashing this class does, we
    /// have a maximum of ~1 trillion transactions supported by the KeyPrefix.
    /// </summary>
    public static readonly TxId MaxPossibleTxId = TxId.From(PartitionId.Transactions.MakeEntityId(0x000000FFFFFFFFFF).Value);

    #endregion
}
