using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.Query.Abstractions.Engines;

public ref struct Environment
{
    public delegate void Execute(ref Environment environment);
    
    private readonly Span<object> _objects;
    private readonly Span<byte> _values;

    public Environment(Span<object> objects, Span<byte> values)
    {
        _objects = objects;
        _values = values;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetObject<T>(int index) where T : class 
        => (T)_objects[index];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetObject<T>(int index, T value) where T : class 
        => _objects[index] = value;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValue<T>(int offset) where T : struct 
        => MemoryMarshal.Read<T>(_values.SliceFast(offset));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue<T>(int offset, T value) where T : struct
        => MemoryMarshal.Write(_values.SliceFast(offset), value);
}
