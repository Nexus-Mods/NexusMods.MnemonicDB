using System;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.BindingConverters;

public class EntityIdConverter : IBindingConverter
{
    public bool CanConvert(Type type, out int priority)
    {
        priority = 1;
        return type == typeof(EntityId);
    }

    public unsafe void Bind<T>(PreparedStatement stmt, int index, T value)
    {
        PreparedStatement.Native.duckdb_bind_uint64(stmt._ptr, index, ((EntityId)(object)value!).Value);
    }
}
