using System;
using System.Reflection;

namespace NexusMods.Query.Abstractions.Engines.Slots;

public struct SlotDefinition()
{
    public Type Type { get; init; }
    public int Offset { get; init; }
    public int Size { get; init; }

    public ISlot<T> MakeSlot<T>()
    {
        if (typeof(T) != Type)
            throw new InvalidOperationException("Type mismatch");

        return SlotFactory(Offset) as ISlot<T> ?? throw new InvalidOperationException("Invalid slot factory");
    }

    private Func<int, object>? _slotFactory = null;
    
    private Func<int, object> SlotFactory => _slotFactory ??= MakeSlotFactory();

    private Func<int, object> MakeSlotFactory()
    {
        var name = Type.IsValueType ? nameof(MakeValueSlot) : nameof(MakeObjectSlot);
        return GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(Type).CreateDelegate<Func<int, object>>();
    }

    private static object MakeValueSlot<T>(int offset) where T : struct
    {
        return new ValueSlot<T>(offset);
    }
    
    private static object MakeObjectSlot<T>(int offset) where T : class
    {
        return new ObjectSlot<T>(offset);
    }
}
