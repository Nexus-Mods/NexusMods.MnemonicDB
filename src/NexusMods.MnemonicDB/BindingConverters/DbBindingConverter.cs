using System;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.BindingConverters;

public class DbBindingConverter : IBindingConverter
{
    public bool CanConvert(Type type, out int priority)
    {
        if (type.IsAssignableTo(typeof(IDb)))
        {
            priority = 1;
            return true;
        }

        priority = 0;
        return false;
    }

    public unsafe void Bind<T>(PreparedStatement stmt, int index, T value)
    {
        IDb db = (IDb)(object)value!;
        var conn = db.Connection;
        var connId = (ushort)conn.DuckDBQueryEngine.IdFor(conn);
        var tx = db.BasisTxId.Value;

        var id = tx << 16 | connId;
        PreparedStatement.Native.duckdb_bind_uint64(stmt._ptr, index, id);
    }
}
