using System;

namespace NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

/// <summary>
/// Wraps another slice, redirecting all calls 
/// </summary>
/// <param name="parent"></param>
/// <typeparam name="TParent"></typeparam>
public struct HistorySlice<TParent>(TParent parent) : ISliceDescriptor 
    where TParent : ISliceDescriptor
{
    public void Reset<T>(T iterator, bool history = false) 
        where T : ILowLevelIterator, allows ref struct
    {
        parent.Reset(iterator, true);
    }

    public bool ShouldContinue(ReadOnlySpan<byte> keySpan, bool history = false)
    {
        return parent.ShouldContinue(keySpan, true);
    }

    public bool IsTotalOrdered => parent.IsTotalOrdered;
}
