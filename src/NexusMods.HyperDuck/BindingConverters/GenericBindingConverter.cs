using System;
using System.Collections.Generic;

namespace NexusMods.HyperDuck.BindingConverters;

public class GenericBindingConverter : IBindingConverter
{
    private static readonly IReadOnlySet<Type> _types = new HashSet<Type> 
    {
        typeof(bool),
        typeof(byte),
        typeof(sbyte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(UInt128),
        typeof(Int128),
        typeof(string)
    };
        
    public bool CanConvert(Type type, out int priority)
    {
        priority = 0;
        return _types.Contains(type);
    }

    public void Bind<T>(PreparedStatement stmt, int index, T value)
    {
        stmt.BindNative(index, value);
    }
}
