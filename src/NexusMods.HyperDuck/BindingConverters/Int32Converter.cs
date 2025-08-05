using System;

namespace NexusMods.HyperDuck.BindingConverters;

public class Int32Converter : IBindingConverter
{
    public bool CanConvert(Type type, out int priority)
    {
        if (type == typeof(int))
        {
            priority = 1;
            return true;
        }
        priority = 0;
        return false;
    }

    public unsafe void Bind<T>(PreparedStatement stmt, int index, T value)
    {
        PreparedStatement.Native.duckdb_bind_int32(stmt._ptr, index, (int)(object)value!);
    }
}
