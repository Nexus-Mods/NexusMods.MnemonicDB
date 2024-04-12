using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NexusMods.MnemonicDB.Abstractions.Internals;


[StructLayout(LayoutKind.Explicit, Size = Size)]
public struct KeyPrefix
{
    /// <summary>
    ///     Fixed size of the KeyPrefix
    /// </summary>
    public const int Size = 12;

    /// <summary>
    /// The maximum inlined value length
    /// </summary>
    public const int MaxLength = LengthOversized - 1;

    /// <summary>
    /// The value to mark that the value is oversized
    /// </summary>
    public const int LengthOversized = 127;


    [FieldOffset(0)] private ushort _attributeId;
    /// <summary>
    /// 4 bits for the partition, 4 bits for the type
    /// </summary>
    [FieldOffset(2)] private byte _typeAndPartition;

    /// <summary>
    /// 1 bit for retract, 7 bits for length
    /// </summary>
    [FieldOffset(3)] private byte _retractAndLength;

    /// <summary>
    /// Entity Id minus the partition
    /// </summary>
    [FieldOffset(4)] private uint _entityId;

    /// <summary>
    /// Transaction Id minus its partition
    /// </summary>
    [FieldOffset(8)] private uint _txId;


    /// <summary>
    ///     The EntityId
    /// </summary>
    public EntityId E
    {
        get => (EntityId)Ids.MakeId(Partition, _entityId);
        set
        {
            _entityId = (uint)value.Value;
            Partition = (byte)(value.Value >> 56);
        }
    }

    /// <summary>
    ///     True if this is a retraction
    /// </summary>
    public bool IsRetract
    {
        get => (_retractAndLength & 0b10000000) != 0;
        set => _retractAndLength = (byte)((_retractAndLength & 0b01111111) | (value ? 0b10000000 : 0));
    }

    /// <summary>
    /// Max of 128 bytes for the value length inlined. If this value is 128 then the real length is stored in the value
    /// as a prefix to the value.
    /// </summary>
    public byte ValueLength
    {
        get => (byte)(_retractAndLength & 0b01111111);
        set => _retractAndLength = (byte)((_retractAndLength & 0b10000000) | (value & 0b01111111));
    }

    /// <summary>
    ///     The attribute id, maximum of 2^16 attributes are supported in the system
    /// </summary>
    public AttributeId A
    {
        get => AttributeId.From(_attributeId);
        set => _attributeId = value.Value;
    }

    /// <summary>
    /// The low level type of the attribute
    /// </summary>
    public LowLevelTypes LowLevelType
    {
        get => (LowLevelTypes)(_typeAndPartition & 0x0F);
        set => _typeAndPartition = (byte)((_typeAndPartition & 0xF0) | ((byte)value & 0x0F));
    }

    /// <summary>
    /// The partition of the entityId
    /// </summary>
    public byte Partition
    {
        get => (byte)(_typeAndPartition >> 4);
        set => _typeAndPartition = (byte)((_typeAndPartition & 0x0F) | (value << 4));
    }


    /// <summary>
    ///     The transaction id, maximum of 2^63 transactions are supported in the system, but really
    ///     it's 2^56 as the upper 8 bits are used for the partition id.
    /// </summary>
    public TxId T
    {
        get => (TxId)Ids.MakeId(Ids.Partition.Tx, _txId);
        set => _txId = (uint)value.Value;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"E: {E.Value:x}, A: {A.Value:x}, T: {T.Value:x}, IsRetract: {IsRetract}, Length: {ValueLength}, Type: {(byte)LowLevelType:X2}";
    }

    /// <summary>
    ///    Deconstructs the key into its parts
    /// </summary>
    public void Deconstruct(out EntityId entityId, out AttributeId attributeId,
        out TxId txId, out bool isRetract, out byte valueLength, out LowLevelTypes lowLevelType)
    {
        entityId = E;
        attributeId = A;
        txId = T;
        isRetract = IsRetract;
        valueLength = ValueLength;
        lowLevelType = LowLevelType;
    }

    /// <summary>
    ///    Deconstructs the key into its parts
    /// </summary>
    public void Deconstruct(out EntityId entityId, out AttributeId attributeId)
    {
        entityId = E;
        attributeId = A;
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

    public static KeyPrefix Min => new KeyPrefix
    {
        _entityId = 0,
        _txId = 0,
        _attributeId = 0,
        _retractAndLength = 0,
        _typeAndPartition = 0,
    };

    public static KeyPrefix Max => new KeyPrefix
    {
        _entityId = uint.MaxValue,
        _txId = uint.MaxValue,
        _attributeId = ushort.MaxValue,
        _retractAndLength = 0xFF,
        _typeAndPartition = 0xFF,
    };
    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new KeyPrefix with the given values, and everything else set to the minimum value
    /// </summary>
    public static implicit operator KeyPrefix(TxId id)
    {
        return new KeyPrefix
        {
            _entityId = 0,
            _attributeId = 0,
            _txId = (uint)id.Value,
            _retractAndLength = 0,
            _typeAndPartition = 0,
        };
    }

    public static implicit operator KeyPrefix(EntityId id)
    {
        return new KeyPrefix
        {
            _typeAndPartition = 0,
            E = id,
            _attributeId = 0,
            _txId = 0,
            _retractAndLength = 0,
        };
    }


    public static implicit operator KeyPrefix(AttributeId id)
    {
        return new KeyPrefix
        {
            _entityId = 0,
            _attributeId = id.Value,
            _txId = 0,
            _retractAndLength = 0,
            _typeAndPartition = 0,
        };
    }






    #endregion

    public bool CouldBeRetractFor(KeyPrefix keyB)
    {
        // Only one can be a retract
        if (!(IsRetract ^ keyB.IsRetract))
            return false;

        // Can't be a retract if it comes first
        if (IsRetract && _txId < keyB._txId)
            return false;

        // Everything else must match
        if (keyB._entityId != _entityId ||
            keyB._attributeId != _entityId ||
            keyB._typeAndPartition != _typeAndPartition ||
            keyB.ValueLength != ValueLength)
            return false;

        // Could be a retract if the value is the same
        return true;
    }
}
