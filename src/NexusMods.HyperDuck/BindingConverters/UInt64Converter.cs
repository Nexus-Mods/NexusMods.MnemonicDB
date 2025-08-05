using System;

namespace NexusMods.HyperDuck.BindingConverters;

public class UInt64Converter : IBindingConverter
{
    public bool CanConvert(Type type, out int priority)
    {
        if (type == typeof(ulong))
        {
            priority = 1;
            return true;
        }
        priority = 0;
        return false;
    }

    public unsafe void Bind<T>(PreparedStatement stmt, int index, T value)
    {
        PreparedStatement.Native.duckdb_bind_uint64(stmt._ptr, index, (ulong)(object)value!);
    }
}
