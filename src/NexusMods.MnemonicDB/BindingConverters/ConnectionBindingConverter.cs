using System;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.BindingConverters;

public class ConnectionBindingConverter : IBindingConverter
{
    public bool CanConvert(Type type, out int priority)
    {
        if (type.IsAssignableTo(typeof(IConnection)))
        {
            priority = 1;
            return true;
        }

        priority = 0;
        return false;
    }

    public unsafe void Bind<T>(PreparedStatement stmt, int index, T value)
    {
        IConnection conn = (IConnection)(object)value!;
        var connId = (ushort)conn.DuckDBQueryEngine.IdFor(conn);
        // Tx of 0 means always the latest
        ulong tx = 0;
        var id = tx << 16 | connId;
        PreparedStatement.Native.duckdb_bind_uint64(stmt._ptr, index, id);
    }
}
