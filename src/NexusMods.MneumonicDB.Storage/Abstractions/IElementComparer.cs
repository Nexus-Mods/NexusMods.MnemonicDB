using System;
using NexusMods.MneumonicDB.Abstractions.Internals;

namespace NexusMods.MneumonicDB.Storage.Abstractions;

public interface IElementComparer<in TAttributeRegistry>
    where TAttributeRegistry : IAttributeRegistry
{
    public static abstract int Compare(TAttributeRegistry registry, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);
}
