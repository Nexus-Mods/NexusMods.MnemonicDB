using System;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// An unsafe version of Datom that relies on pointers
/// </summary>
public unsafe ref struct RawDatom : IComparable<RawDatom>
{
    public byte* _key;
    public int _keySize;

    /// <inheritdoc />
    public int CompareTo(RawDatom other)
    {
        return ValueComparer.Compare(_key, _keySize, other._key, other._keySize);
    }
}
