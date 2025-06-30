using DuckDB.NET.Native;

namespace NexusMods.MnemonicDB.QueryV2;

public interface IDataChunk<TResultType, TThis>
    where TThis : IDataChunk<TResultType, TThis>
{
    public static abstract bool TryCreate(ref DuckDBResult result, out TThis success);

    public bool TryGetRow(int offset, out TResultType row);
}
