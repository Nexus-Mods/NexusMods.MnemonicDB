using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.MnemonicDB.Abstractions.UnsafeIterators;

public interface IUnsafeDatomIterable<TIterator> where TIterator : IUnsafeIterator, allows ref struct
{
    public TIterator IterateFrom<TDatom>(TDatom from) where TDatom : IUnsafeDatom;
}

public static class UnsafeDatomIterableExtensions
{
    public static IndexSegment IndexSegment<TFrom, TTo, TIterable, TIterator>(this TIterable iterable, TFrom from, TTo to)
        where TIterable : IUnsafeDatomIterable<TIterator>
        where TIterator : IUnsafeIterator, allows ref struct
        where TFrom : IUnsafeDatom
        where TTo : IUnsafeDatom
    {
        using var builder = new IndexSegmentBuilder();
        using var iterator = iterable.IterateFrom(from);
        
        for (; iterator.To(to); iterator.Next()) 
            builder.Add(iterator);
        
        return builder.Build();
    }
}
