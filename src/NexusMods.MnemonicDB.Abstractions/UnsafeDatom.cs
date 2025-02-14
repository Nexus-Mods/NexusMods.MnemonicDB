using System;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.UnsafeIterators;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// An unsafe version of Datom that relies on pointers
/// </summary>
public unsafe struct UnsafeDatom
{
    public byte* _key;
    public int _keySize;

    /// <inheritdoc />
    public int CompareTo<TDatom>(TDatom other) where TDatom : IUnsafeDatom, allows ref struct
    {
        return ValueComparer.Compare(_key, _keySize, other.Key, other.KeySize);
    }
}
