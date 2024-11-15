using System;

namespace NexusMods.MnemonicDB.Abstractions.Iterators;

/// <summary>
/// A ref datom iterator 
/// </summary>
public interface IRefDatomIterator : IDisposable
{
    /// <summary>
    /// Move to the next datom in the iterator if possible and return true, otherwise return false
    /// </summary>
    public bool MoveNext(out RefDatom datom);
}
