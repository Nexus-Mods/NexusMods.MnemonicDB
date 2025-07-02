using DuckDB.NET.Data;

namespace NexusMods.MnemonicDB.Abstractions;

public interface IQueryFunction
{
    public void Register(DuckDBConnection connection, IQueryEngine engine);
}
