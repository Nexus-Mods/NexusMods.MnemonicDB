---
hide:
  - toc
---

## Value Format

Originally, MnemonicDB was developed using out-of-band value type serialization. That is to say, each attribute had a C# name
attached to it, that would be used at read time to determine the format of the value. This method was simple, but had several
side effects. For one, it was impossible to read the data without having access to that C# class. Since RocksDB performs
some value comparisons at startup after a crash, this resulted in an unreadable database. The RocksDB couldn't start because
it needed a comparator, but the comparator couldn't start until RocksDB had properly started and the comparator could read
all the possible value types. Thus the internals of MnemonicDB were rewritten to use a more standard format.

### Value Format
Every value in the system is prefixed by a one-byte type identifier. This identifier determines the size, value type, and
serialization format. As of the time of this writing we are only using about 16 of the possible 256 values, so there is
plenty of room for expansion. These formats are stored in the `ValueTags` enum, and currently support the following values:

```csharp
public enum ValueTags : byte
{
    /// <summary>
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
}
```

Many of these values have a fixed size and are self-describing. Since the format is so simple, we can "serialize" data 
such as integers by doing a simple pointer dereference. For other more complex values like strings, we must run them 
through a text encoder/decoder. None of the variable sized values have an encoded length, this is because RocksDB tracks
value sizes, so it can be assumed that every key is a 16 byte header, followed by a ValueTag, followed by the value, with 
the value taking up the remainder of the key. 

### Comparator Simplicity
Since this value/key format is so simple it's possible in a few hundred lines of code to write a comparator for RocksDB
to sort this data. In MnemonicDB the comparator is a completely static method without any virtual dispatch in the main-line
code. In the future it would be fairly simple to move the comparator code into a C++ DLL to further squeeze up some performance,
but this is considered a low priority at the moment.