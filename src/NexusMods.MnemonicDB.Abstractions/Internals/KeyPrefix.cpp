#include <stdexcept>
#include <string>
#include <cstdint>
#include <cstring>
#include <span>

struct KeyPrefix
{
    // Fixed size of the KeyPrefix
    static constexpr int Size = 16;

    uint64_t Upper;
    uint64_t Lower;

    // Sets the key prefix to the given values
    KeyPrefix(uint64_t upper, uint64_t lower) : Upper(upper), Lower(lower) {}

    // Disallow the default constructor
    KeyPrefix() 
    {
        throw std::invalid_argument("This constructor should not be called, use the Create method instead");
    }

    // Creates a new KeyPrefix with the given values
    KeyPrefix(uint64_t id, uint16_t attributeId, uint64_t txId, bool isRetract, uint8_t tags, uint8_t index = 0)
    {
        Upper = (static_cast<uint64_t>(attributeId) << 48) | (txId & 0x000000FFFFFFFFFF) | (static_cast<uint64_t>(index) << 40);
        Lower = (id & 0xFF00000000000000) | ((id & 0x0000FFFFFFFFFFFF) << 8) | (isRetract ? 1ULL : 0ULL) | (static_cast<uint64_t>(tags) << 1);
    }

    // The index this datom is stored in. This will be 0 (None)
    // for most datoms seen by the user, but the backend datomstore
    // will use this field for the various sorted indexes.
    uint8_t Index() const
    {
        return static_cast<uint8_t>((Upper >> 40) & 0xFF);
    }

    void SetIndex(uint8_t value)
    {
        Upper = (Upper & 0xFFFF00FFFFFFFFFF) | (static_cast<uint64_t>(value) << 40);
    }

    // The EntityId
    uint64_t E() const
    {
        return (Lower & 0xFF00000000000000) | ((Lower >> 8) & 0x0000FFFFFFFFFFFF);
    }

    void SetE(uint64_t value)
    {
        Lower = (Lower & 0xFF) | (value & 0xFF00000000000000) | ((value & 0x0000FFFFFFFFFFFF) << 8);
    }

    // True if this is a retraction
    bool IsRetract() const
    {
        return (Lower & 1) == 1;
    }

    void SetIsRetract(bool value)
    {
        Lower = value ? (Lower | 1ULL) : (Lower & ~1ULL);
    }

    // The value tag of the datom, which defines the actual value type of the datom
    uint8_t ValueTag() const
    {
        return static_cast<uint8_t>((Lower >> 1) & 0x7F);
    }

    void SetValueTag(uint8_t value)
    {
        Lower = (Lower & 0xFFFFFFFFFFFFFF01) | (static_cast<uint64_t>(value) << 1);
    }

    // The attribute id, maximum of 2^16 attributes are supported in the system
    uint16_t A() const
    {
        return static_cast<uint16_t>(Upper >> 48);
    }

    void SetA(uint16_t value)
    {
        Upper = (Upper & 0x0000FFFFFFFFFFFF) | (static_cast<uint64_t>(value) << 48);
    }

    // The transaction id, maximum of 2^63 transactions are supported in the system, but really
    // it's 2^56 as the upper 8 bits are used for the partition id.
    uint64_t T() const
    {
        return Upper & 0x000000FFFFFFFFFF;
    }

    void SetT(uint64_t value)
    {
        Upper = (Upper & 0xFFFFFF0000000000) | (value & 0x000000FFFFFFFFFF);
    }

    // String representation
    std::string ToString() const
    {
        return "E: " + std::to_string(E()) + ", A: " + std::to_string(A()) +
               ", T: " + std::to_string(T()) + ", Retract: " + (IsRetract() ? "true" : "false");
    }

    // Gets the KeyPrefix from the given bytes
    static KeyPrefix Read(std::span<const uint8_t> bytes)
    {
        KeyPrefix prefix;
        std::memcpy(&prefix, bytes.data(), Size);
        return prefix;
    }

    // The minimum key prefix possible
    static const KeyPrefix Min;

    // The maximum key prefix possible
    static const KeyPrefix Max;

    // Returns true if this key is valid
    bool IsValid() const
    {
        return Upper != 0;
    }
};

constexpr KeyPrefix KeyPrefix::Min = KeyPrefix(0, 0);
constexpr KeyPrefix KeyPrefix::Max = KeyPrefix(UINT64_MAX, UINT64_MAX);