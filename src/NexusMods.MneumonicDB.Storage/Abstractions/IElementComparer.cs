using System;

namespace NexusMods.MneumonicDB.Storage.Abstractions;

public interface IElementComparer
{
    public static abstract int Compare(AttributeRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);
}
