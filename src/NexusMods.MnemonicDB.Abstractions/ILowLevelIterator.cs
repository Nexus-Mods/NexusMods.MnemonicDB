using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;
using Reloaded.Memory.Pointers;

namespace NexusMods.MnemonicDB.Abstractions;

public interface ILowLevelIterator : IDisposable
{
    /// <summary>
    /// Move the iterator to the first datom that matches the given span.
    /// </summary>
    /// <param name="span"></param>
    public void SeekTo(scoped ReadOnlySpan<byte> span);

    /// <summary>
    /// Move the iterator to the first datom before the given span.
    /// </summary>
    public void SeekToPrev(scoped ReadOnlySpan<byte> span);
    
    /// <summary>
    /// Move to the next datom
    /// </summary>
    /// <returns></returns>
    public void Next();
    
    /// <summary>
    /// Move to the previous datom
    /// </summary>
    public void Prev();
    
    /// <summary>
    /// Return true if the iterator is valid.
    /// </summary>
    public bool IsValid { get; }
    
    /// <summary>
    /// Get the key span of the current datom.
    /// </summary>
    public Ptr Key { get; }
    
    /// <summary>
    /// Get the extra value span 
    /// </summary>
    public Ptr Value { get; }
}

public static class LowLevelIteratorExtensions
{
    public static LowLevelIteratorEnumerator<TLowLevelIterator, TDescriptor> Slice<TLowLevelIterator, TDescriptor>(this TLowLevelIterator iterator, TDescriptor descriptor) 
        where TLowLevelIterator : ILowLevelIterator
        where TDescriptor : ISliceDescriptor
    {
        return new LowLevelIteratorEnumerator<TLowLevelIterator, TDescriptor>(iterator, descriptor);
    }
}

public struct LowLevelIteratorEnumerator<TLowLevelIterator, TDescriptor> : IRefDatomEnumerator
    where TLowLevelIterator : ILowLevelIterator
    where TDescriptor : ISliceDescriptor
{
    private TLowLevelIterator _iterator;
    private TDescriptor _descriptor;
    private bool _started;
    private Ptr _currentKey;

    public LowLevelIteratorEnumerator(TLowLevelIterator iterator, TDescriptor descriptor)
    {
        _iterator = iterator;
        _descriptor = descriptor;
        _started = false;
    }

    public bool MoveNext()
    {
        if (_started == false)
        {
            _descriptor.Reset(_iterator);
            _started = true;
        }
        else
        {
            _descriptor.MoveNext(_iterator);
        }
            
        if (_iterator.IsValid)
        {
            _currentKey = _iterator.Key;
            return _descriptor.ShouldContinue(_currentKey.Span);
        }
        return false;
    }

    public KeyPrefix KeyPrefix => _currentKey.Read<KeyPrefix>(0);
    public ReadOnlySpan<byte> ValueSpan => _currentKey.Span.SliceFast(KeyPrefix.Size);

    public ReadOnlySpan<byte> ExtraValueSpan 
    {
        get
        {
            Debug.Assert(KeyPrefix.ValueTag == ValueTag.HashedBlob, "ExtraValueSpan is only valid for HashedBlob values");
            return _iterator.Value.Span;
        }
    }
}
