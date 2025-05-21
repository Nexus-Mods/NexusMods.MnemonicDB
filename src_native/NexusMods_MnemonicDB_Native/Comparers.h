//
// Created by tbald on 5/20/2025.
//

#ifndef ECOMPARER_H
#define ECOMPARER_H
#include "datom.h"
#include "stdint.h"

#include <compare>

/**
 * Got to love C++, this uses the new spaceship operator to compare two
 * values, and returns -1, 0, or 1 depending on the comparison. Although this
 * looks like a lot of code, it compiles down to just a few instructions and is
 * branchless.
 */
template<typename T, typename U>
constexpr int cmp(const T& a, const U& b) {
    auto cmp = a <=> b;
    if (cmp == std::strong_ordering::less)    return -1;
    if (cmp == std::strong_ordering::greater) return  1;
    return 0;
}


struct Comparers
{
    static constexpr uint64_t EMask = 0xFFFFFFFFFFFFFF00ULL;

    static int Compare(const rocksdb::Slice& aPtr, const rocksdb::Slice& bPtr)
    {
        const auto aVal = reinterpret_cast<const KeyPrefix *>(aPtr.data())->Lower & EMask;
        const auto bVal = reinterpret_cast<const KeyPrefix *>(bPtr.data())->Lower & EMask;
        return cmp(aVal, bVal);
    }
};

struct AComparer
{
    static constexpr uint64_t AMask = 0xFFFF000000000000ULL;

    static int Compare(const rocksdb::Slice& aPtr, const rocksdb::Slice& bPtr)
    {
        const auto aVal = reinterpret_cast<const KeyPrefix *>(aPtr.data())->Upper & AMask;
        const auto bVal = reinterpret_cast<const KeyPrefix *>(bPtr.data())->Upper & AMask;
        return cmp(aVal, bVal);
    }
};

struct TComparer
{
    static constexpr uint64_t TMask = 0x000000FFFFFFFFFFULL;

    static int Compare(const rocksdb::Slice& aPtr, const rocksdb::Slice& bPtr)
    {
        const auto aVal = reinterpret_cast<const KeyPrefix *>(aPtr.data())->Upper & TMask;
        const auto bVal = reinterpret_cast<const KeyPrefix *>(bPtr.data())->Upper & TMask;
        return cmp(aVal, bVal);
    }
};

struct ReferenceComparer
{
    static int Compare(const rocksdb::Slice& aPtr, const rocksdb::Slice& bPtr)
    {
        const auto aVal = *reinterpret_cast<const EntityId *>(aPtr.data() + KeyPrefix::Size);
        const auto bVal = *reinterpret_cast<const EntityId *>(bPtr.data() + KeyPrefix::Size);
        return cmp(aVal, bVal);
    }
};

struct ValueComparer
{
    static int Compare(const rocksdb::Slice& aPtr, const rocksdb::Slice& bPtr)
    {
        const auto typeA = reinterpret_cast<const KeyPrefix *>(aPtr.data())->ValueTag();
        const auto typeB = reinterpret_cast<const KeyPrefix *>(bPtr.data())->ValueTag();
        auto typeAByte = static_cast<uint8_t>(typeA);
        auto typeBByte = static_cast<uint8_t>(typeB);

        if (typeAByte != typeBByte)
            return typeAByte < typeBByte ? -1 : 1;

        return Compare(typeA, aPtr, bPtr);
    }

    static int Compare(ValueTags type, const rocksdb::Slice& aPtr, const rocksdb::Slice& bPtr)
    {

        switch (type)
        {
            case Null:
                return 0;
            case UInt8:
                return ReinterpretCompare<uint8_t>(aPtr, bPtr);
            case UInt16:
                return ReinterpretCompare<uint16_t>(aPtr, bPtr);
            case UInt32:
                return ReinterpretCompare<uint32_t>(aPtr, bPtr);
            case UInt64:
                return ReinterpretCompare<uint64_t>(aPtr, bPtr);
            case Int16:
                return ReinterpretCompare<int16_t>(aPtr, bPtr);
            case Int32:
                return ReinterpretCompare<int32_t>(aPtr, bPtr);
            case Int64:
                return ReinterpretCompare<int64_t>(aPtr, bPtr);
            case UInt128:
                return ReinterpretCompare<__uint128_t>(aPtr, bPtr);
            case Int128:
                return ReinterpretCompare<__int128_t>(aPtr, bPtr);
            case Float32:
                return ReinterpretCompare<float>(aPtr, bPtr);
            case Float64:
                return ReinterpretCompare<double>(aPtr, bPtr);
            case Ascii:
                return CompareMemory(aPtr, bPtr);
            case Utf8:
                return CompareMemory(aPtr, bPtr);
            case Utf8Insensitive:
                break;
            case Blob:
                return CompareMemory(aPtr, bPtr);
            case HashedBlob:
                return ReinterpretCompare<EntityId>(aPtr, bPtr);
            case Reference:
                return ReinterpretCompare<EntityId>(aPtr, bPtr);
            case Tuple3_Ref_UShort_Utf8I:
                break;
            case Tuple2_UShort_Utf8I:
                break;
        }
        return 0;
    }

    static int CompareMemory(rocksdb::Slice aPtr, rocksdb::Slice bPtr)
    {
        auto aSize = aPtr.size();
        auto bSize = bPtr.size();
        auto cmpLen = std::min(aSize, bSize);
        auto cmp = memcmp(aPtr.data(), bPtr.data(), cmpLen);
        if (cmp != 0)
            return cmp;
        if (aSize < bSize)
            return -1;
        return 1;
    }


    template<class T>
    static int ReinterpretCompare(const rocksdb::Slice& aPtr, const rocksdb::Slice& bPtr)
    {
        auto a = *reinterpret_cast<const T*>(aPtr.data() + KeyPrefix::Size);
        auto b = *reinterpret_cast<const T*>(bPtr.data() + KeyPrefix::Size);
        return cmp(a, b);
    }
};


template<class A, class B, class C, class D>
struct ADatomComparator
{
    static int Compare(const rocksdb::Slice& aPtr, const rocksdb::Slice& bPtr)
    {
        auto cmp = A::Compare(aPtr, bPtr);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = B::Compare(aPtr, bPtr);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = C::Compare(aPtr, bPtr);
        if (cmp != 0)
        {
            return cmp;
        }

        return D::Compare(aPtr, bPtr);
    }
};

typedef ADatomComparator<TComparer, Comparers, AComparer, ValueComparer> TxLogComparator;
typedef ADatomComparator<Comparers, AComparer, ValueComparer, TComparer> EAVTComparator;
typedef ADatomComparator<AComparer, ValueComparer, Comparers, TComparer> AEVTComparator;
typedef ADatomComparator<AComparer, Comparers, ValueComparer, TComparer> AVETComparator;
typedef ADatomComparator<ReferenceComparer, AComparer, Comparers, TComparer> VAETComparator;

#endif //ECOMPARER_H
