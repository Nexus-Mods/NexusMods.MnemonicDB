//
// Created by tbald on 5/20/2025.
//

#ifndef DATOM_H
#define DATOM_H
#include <stdint.h>

enum IndexType
{
    /// <summary>
    /// Default for datoms that are not part of an index
    /// </summary>
    None = 0,

    /// <summary>
    /// Transaction log index
    /// </summary>
    TxLog = 1,

    /// <summary>
    /// Current row-level index
    /// </summary>
    EAVTCurrent = 2,

    /// <summary>
    /// History row-level index
    /// </summary>
    EAVTHistory,

    /// <summary>
    /// Current column-level index
    /// </summary>
    AEVTCurrent,

    /// <summary>
    /// History column-level index
    /// </summary>
    AEVTHistory,

    /// <summary>
    ///  Current reverse reference index
    /// </summary>
    VAETCurrent,

    /// <summary>
    /// History reverse reference index
    /// </summary>
    VAETHistory,

    /// <summary>
    /// Current indexed value index
    /// </summary>
    AVETCurrent,

    /// <summary>
    /// History indexed value index
    /// </summary>
    AVETHistory
};

enum ValueTags
{
    // <summary>
    /// Null value, no data
    /// </summary>
    Null = 0,
    /// <summary>
    /// Unsigned 8-bit integer
    /// </summary>
    UInt8 = 1,
    /// <summary>
    /// Unsigned 16-bit integer
    /// </summary>
    UInt16 = 2,
    /// <summary>
    /// Unsigned 32-bit integer
    /// </summary>
    UInt32 = 3,
    /// <summary>
    /// Unsigned 64-bit integer
    /// </summary>
    UInt64 = 4,
    /// <summary>
    /// Unsigned 128-bit integer
    /// </summary>
    UInt128 = 5,
    /// <summary>
    /// Unsigned 16-bit integer
    /// </summary>
    Int16 = 6,
    /// <summary>
    /// Unsigned 32-bit integer
    /// </summary>
    Int32 = 7,
    /// <summary>
    /// Unsigned 64-bit integer
    /// </summary>
    Int64 = 8,
    /// <summary>
    /// Unsigned 128-bit integer
    /// </summary>
    Int128 = 9,
    /// <summary>
    /// 32-bit floating point number
    /// </summary>
    Float32 = 10,
    /// <summary>
    /// 64-bit floating point number (double)
    /// </summary>
    Float64 = 11,
    /// <summary>
    /// ASCII string, case-sensitive
    /// </summary>
    Ascii = 12,
    /// <summary>
    /// UTF-8 string, case-sensitive
    /// </summary>
    Utf8 = 13,
    /// <summary>
    /// UTF-8 string, case-insensitive
    /// </summary>
    Utf8Insensitive = 14,
    /// <summary>
    /// Inline binary data
    /// </summary>
    Blob = 15,

    /// <summary>
    /// A blob sorted by its xxHash64 hash, and where the data is possibly stored in a separate location
    /// as to degrade the performance of the key storage
    /// </summary>
    HashedBlob = 16,

    /// <summary>
    /// A reference to another entity
    /// </summary>
    Reference = 17,

    /// <summary>
    /// A tuple of three values: a reference, an unsigned 16-bit integer, and a UTF-8 (case insensitive) string
    /// </summary>
    Tuple3_Ref_UShort_Utf8I = 64,

    /// <summary>
    /// A tuple of two values: an unsigned 16-bit integer and a UTF-8 (case insensitive) string
    /// </summary>
    Tuple2_UShort_Utf8I = 65,
};

typedef uint64_t EntityId;
typedef uint16_t AttributeId;
typedef uint64_t TxId;


struct KeyPrefix
{
    static constexpr int Size = 16;

    public:
       uint64_t Upper;
       uint64_t Lower;

    KeyPrefix(uint64_t upper, uint64_t lower)
    {
        Upper = upper;
        Lower = lower;
    }

    KeyPrefix(EntityId id, AttributeId attributeId, TxId txId, bool isRetract, ValueTags tags, IndexType index)
    {
        Upper = (static_cast<uint64_t>(attributeId) << 48) | (txId & 0x000000FFFFFFFFFF) | (static_cast<uint64_t>(index) << 40);
        Lower = (id & 0xFF00000000000000) | ((id & 0x0000FFFFFFFFFFFF) << 8) | (isRetract ? 1ULL : 0ULL) | (static_cast<uint64_t>(tags) << 1);
    }

    [[nodiscard]]
    IndexType Index() const
    {
        return static_cast<IndexType>((Upper >> 40) & 0x00000000000000FF);
    }

    EntityId E() const
    {
        return (Lower & 0xFF00000000000000) | ((Lower >> 8) & 0x0000FFFFFFFFFFFF);
    }

    void SetE(const EntityId value)
    {
        Lower = (Lower & 0xFF) | (value & 0xFF00000000000000) | ((value & 0x0000FFFFFFFFFFFF) << 8);
    }

    [[nodiscard]]
    bool IsRetract() const
    {
        return (Lower & 1) == 1;
    }

    void SetIsRetract(const bool value)
    {
        Lower = value ? (Lower | 1ULL) : (Lower & ~1ULL);
    }

    [[nodiscard]]
    ValueTags ValueTag() const
    {
        return static_cast<ValueTags>((Lower >> 1) & 0x7F);
    }

    void SetValueTag(const uint8_t value)
    {
        Lower = (Lower & 0xFFFFFFFFFFFFFF01) | (static_cast<uint64_t>(value) << 1);
    }

    uint16_t A() const
    {
        return static_cast<uint16_t>(Upper >> 48);
    }

    void SetA(uint16_t value)
    {
        Upper = (Upper & 0x0000FFFFFFFFFFFF) | (static_cast<uint64_t>(value) << 48);
    }

    [[nodiscard]]
    TxId T() const
    {
        return Upper & 0x000000FFFFFFFFFF;
    }

    void SetT(const TxId value)
    {
        Upper = (Upper & 0xFFFFFF0000000000) | (value & 0x000000FFFFFFFFFF);
    }

    [[nodiscard]]
    bool IsValid() const
    {
        return Upper != 0;
    }
};

#endif //DATOM_H
