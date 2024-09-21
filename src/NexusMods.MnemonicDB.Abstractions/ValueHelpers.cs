using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Static methods that help with reading, writing and formatting values
/// </summary>
public static class ValueHelpers
{
    /// <summary>
    /// Remaps the values of this attribute, if required. 
    /// </summary>
    public static void Remap(Func<EntityId, EntityId> remapFn, in KeyPrefix prefix, Span<byte> valueSpan)
    {
        switch (prefix.ValueTag)
        {
            case ValueTags.Reference:
                var oldId = MemoryMarshal.Read<EntityId>(valueSpan);
                var newId = remapFn(oldId);
                MemoryMarshal.Write(valueSpan, newId);
                break;
            case ValueTags.Tuple2:
            {
                var tag1 = (ValueTags)valueSpan[0];
                var tag2 = (ValueTags)valueSpan[1];
                if (tag1 == ValueTags.Reference)
                {
                    var entityId = MemoryMarshal.Read<EntityId>(valueSpan.SliceFast(2));
                    var newEntityId = remapFn(entityId);
                    MemoryMarshal.Write(valueSpan.SliceFast(2), newEntityId);
                }
                if (tag2 == ValueTags.Reference)
                {
                    throw new NotSupportedException("This attribute does not support remapping of the second element.");
                }
                break;
            }
            case ValueTags.Tuple3:
            {
                var tag1 = (ValueTags)valueSpan[0];
                var tag2 = (ValueTags)valueSpan[1];
                var tag3 = (ValueTags)valueSpan[2];
                if (tag1 == ValueTags.Reference)
                {
                    var entityId = MemoryMarshal.Read<EntityId>(valueSpan.SliceFast(3));
                    var newEntityId = remapFn(entityId);
                    MemoryMarshal.Write(valueSpan.SliceFast(3), newEntityId);
                }
                if (tag2 == ValueTags.Reference)
                {
                    throw new NotSupportedException("This attribute does not support remapping of the second element.");
                }
                if (tag3 == ValueTags.Reference)
                {
                    throw new NotSupportedException("This attribute does not support remapping of the third element.");
                }
                break;
            } 
        }
    }

}
