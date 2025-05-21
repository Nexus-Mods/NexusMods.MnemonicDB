#include "library.h"
#include <iostream>
#include <memory>
#include "rocksdb/comparator.h"
#include "rocksdb/slice_transform.h"
#include "rocksdb/db.h"
#include "datom.h"
#include "Comparers.h"

class Comparator final : public rocksdb::Comparator {

    static constexpr uint64_t IndexMask = 0xFFULL << 40;

public:
    [[nodiscard]]
    int Compare(const rocksdb::Slice& a, const rocksdb::Slice& b) const override
    {
        auto aIndex = reinterpret_cast<const KeyPrefix*>(a.data())->Upper & IndexMask;
        auto bIndex = reinterpret_cast<const KeyPrefix*>(b.data())->Upper & IndexMask;
        if (aIndex != bIndex)
        {
            return aIndex < bIndex ? -1 : 1;
        }

        switch (static_cast<IndexType>(aIndex >> 40))
        {
            case TxLog:
                return TxLogComparator::Compare(a, b);
            case EAVTCurrent:
            case EAVTHistory:
                return EAVTComparator::Compare(a, b);
            case AEVTCurrent:
            case AEVTHistory:
                return AEVTComparator::Compare(a, b);
            case AVETCurrent:
            case AVETHistory:
                return AVETComparator::Compare(a, b);
            case VAETCurrent:
            case VAETHistory:
                return VAETComparator::Compare(a, b);
            default:
                break;
        }

        return EAVTComparator::Compare(a, b);
    }

    [[nodiscard]]
    const char* Name() const override {
        return "GlobalCompare";
    }

    void FindShortestSeparator(std::string *start, const rocksdb::Slice &limit) const override
    {

    }

    void FindShortSuccessor(std::string *key) const override
    {

    }
};

class PrefixExtractor final : public rocksdb::SliceTransform
{
    static constexpr uint64_t PrefixSize = 8;

    [[nodiscard]]
    const char* Name() const override {
        return "ExtractorV1";
    }

    [[nodiscard]]
    bool InDomain(const rocksdb::Slice& key) const override {
        return key.size() >= PrefixSize;
    }

    [[nodiscard]]
    rocksdb::Slice Transform(const rocksdb::Slice& src) const override {
        auto blob = new char[PrefixSize];
        rocksdb::Slice data(blob, PrefixSize);

        auto prefix = reinterpret_cast<const KeyPrefix*>(src.data());
        auto indexType = prefix->Index();

        uint64_t outValue = (static_cast<uint64_t>(indexType) << 56);

        switch (indexType)
        {
            case EAVTCurrent:
            case EAVTHistory:
                // Extract the EntityId from the prefix
                outValue |= prefix->Lower >> 8;
                break;
            case AEVTCurrent:
            case AEVTHistory:
            case AVETCurrent:
            case AVETHistory:
                // Only need the AttributeId
                outValue |= prefix->A();
                break;
            case VAETCurrent:
            case VAETHistory: {
                // Extract the EntityId from the value portion of the prefix
                EntityId eid = *reinterpret_cast<const EntityId *>(src.data() + KeyPrefix::Size);
                // Now we need to pack it by removing the left-but-one byte from the ulong
                uint64_t packed = ((eid & 0xFF00000000000000ULL) >> 8) | (eid & 0x0000FFFFFFFFFFFFULL);
                outValue |= packed;
            }
                break;
            case TxLog:
                // Extract the TxId
                outValue |= prefix->Upper & 0x000000FFFFFFFFFFULL;
                break;

        }

        reinterpret_cast<uint64_t*>(blob)[0] = outValue;

        return data;
    }
};

void* mnemonicdb_open(const char* path, const bool in_memory, const bool readonly) {
    rocksdb::Options options;

    options.create_if_missing = true;
    options.create_missing_column_families = true;
    options.compression = rocksdb::kZSTD;

    options.comparator = new Comparator();
    options.prefix_extractor = std::make_shared<PrefixExtractor>();


    if (in_memory)
        options.env = rocksdb::NewMemEnv(rocksdb::Env::Default());
    else
        options.env = rocksdb::Env::Default();


    rocksdb::DB* db;
    rocksdb::Status status;

    if (readonly) {
        status = rocksdb::DB::Open(options, path, &db);
    } else {
        status = rocksdb::DB::OpenForReadOnly(options, path, &db);
    }

    if (!status.ok()) {
        std::cerr << "Unable to open database: " << status.ToString() << std::endl;
        return nullptr;
    }

    return db;
}