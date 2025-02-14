using System;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.UnsafeIterators;

public interface IUnsafeIterator : IDisposable
{
    public UnsafeDatom Current { get; }
    
    public ReadOnlySpan<byte> CurrentExtraValue { get; }
    
    public bool IsValid { get; }
    
    public void Next();
}

public static class UnsafeIteratorExtensions
{
    public static bool To<TIterator, TDatom>(this TIterator iterator, in TDatom to) 
        where TIterator : IUnsafeIterator, allows ref struct
        where TDatom : IUnsafeDatom, allows ref struct
    {
        return iterator.IsValid && iterator.Current.CompareTo(to) <= 0;
    }
    
    /// <summary>
    /// Gets the current ValueTag of the iterator
    /// </summary>
    public static unsafe ValueTag ValueTag<TIterator>(this TIterator iterator) 
        where TIterator : IUnsafeIterator, allows ref struct
    {
        return ((KeyPrefix*)iterator.Current._key)->ValueTag;
    }
    
    public static unsafe ReadOnlySpan<byte> GetKeySpan<TIterator>(this TIterator iterator) where TIterator : IUnsafeIterator, allows ref struct
    {
        return new(iterator.Current._key, iterator.Current._keySize);
    }
}
 
