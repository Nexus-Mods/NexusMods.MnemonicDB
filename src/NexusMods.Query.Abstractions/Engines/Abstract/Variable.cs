using System;
using System.Threading;
using NexusMods.Query.Abstractions.Engines.Slots;

namespace NexusMods.Query.Abstractions.Engines.Abstract;

internal static class IdGenerator
{
    private static ulong _id = 0;

    public static ulong NextId() => Interlocked.Increment(ref _id);
}

/// <summary>
/// A variable that will be populated by the engine as it executes
/// </summary>
public record Variable<T>(string Name, ulong Id) : IVariable
{
    public static Variable<T> New(string name = "")
    {
        return new Variable<T>(name, IdGenerator.NextId());
    }

    public override string ToString()
    {
        return $"?{Name}<{typeof(T).Name}>({Id})";
    }

    public Type Type => typeof(T);
    
    public SlotDefinition MakeSlotDefinition(ref int objectOffset, ref int valueOffset)
    {
        if (typeof(T).IsValueType)
        {
            var size = System.Runtime.InteropServices.Marshal.SizeOf<T>();
            var definition = new SlotDefinition()
            {
                Type = typeof(T),
                Offset = valueOffset,
                Size = size
            };
            valueOffset += size;
            return definition;
        }
        else
        {
            var definition = new SlotDefinition
            {
                Type = typeof(T),
                Offset = objectOffset,
                Size = IntPtr.Size
            };
            objectOffset++;
            return definition;
        }
    }

}
