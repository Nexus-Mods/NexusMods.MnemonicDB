using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.EventSourcing.Storage.Abstractions;

namespace NexusMods.EventSourcing.Storage.InMemoryBackend;

public class Index<TA, TB, TC, TD, TF>(AttributeRegistry registry, IndexStore store) :
    AIndex<TA, TB, TC, TD, TF, IndexStore>(registry, store), IInMemoryIndex, IComparer<byte[]>
    where TA : IElementComparer
    where TB : IElementComparer
    where TC : IElementComparer
    where TD : IElementComparer
    where TF : IElementComparer
{
    public int Compare(byte[]? x, byte[]? y)
    {
        return Compare(x.AsSpan(), y.AsSpan());
    }

    public ImmutableSortedSet<byte[]> Set => store.Set;
}
