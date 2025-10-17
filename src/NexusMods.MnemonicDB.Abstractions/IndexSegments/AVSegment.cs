using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Traits;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

public readonly struct AVSegment
{
    public AttributeId[] AttributeIds { get; init; }
    public object[] Values { get; init; }
    
    public AVSegment(Datoms datoms, AttributeResolver resolver)
    {
        AttributeIds = GC.AllocateUninitializedArray<AttributeId>(datoms.Count);
        Values = GC.AllocateUninitializedArray<object>(datoms.Count);

        for (var i = 0; i < datoms.Count; i++)
        {
            var d = datoms[i];
            if (!resolver.TryGetAttribute(d.A, out var attr))
                continue;
            AttributeIds[i] = d.A;
            Values[i] = attr.FromLowLevelObject(d.Value, resolver);
        }
    }
}
