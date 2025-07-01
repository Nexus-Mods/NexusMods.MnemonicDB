using DuckDB.NET.Native;

namespace NexusMods.MnemonicDB.QueryV2;

public struct DataChunk<TVector1, T1> : IDataChunk<T1, DataChunk<TVector1, T1>>
    where TVector1 : IVector<T1, TVector1>
{
    private readonly ulong _rowCount;
    private readonly IVector<T1, TVector1> _column0Vector;

    public DataChunk(DuckDBDataChunk chunk)
    {
        _rowCount = NativeMethods.DataChunks.DuckDBDataChunkGetSize(chunk);
        _column0Vector = TVector1.Create(chunk, 0);
    }

    public void Reset(DuckDBDataChunk chunk)
    {
        _column0Vector.Reset(chunk, 0);
    }

    public static bool TryCreate(ref DuckDBResult result, out DataChunk<TVector1, T1> chunk)
    {
        var c = NativeMethods.Query.DuckDBFetchChunk(result);
        if (c.IsInvalid)
        {
            chunk = default;
            return false;
        }

        chunk = new DataChunk<TVector1, T1>(c);
        return true;
    }

    public bool TryGetRow(int offset, out T1 value)
    {
        if (offset >= (int)_rowCount || offset < 0)
        {
            value = default!;
            return false;
        }
        value = _column0Vector.GetLowLevel(offset);
        return true;
    }

}

public struct DataChunk<TVector1, TVector2, T1, T2> : IDataChunk<(T1, T2), DataChunk<TVector1, TVector2, T1, T2>>
    where TVector1 : IVector<T1, TVector1>
    where TVector2 : IVector<T2, TVector2>
{
    private readonly ulong _rowCount;
    private readonly IVector<T1, TVector1> _column0Vector;
    private readonly IVector<T2, TVector2> _column1Vector;

    public DataChunk(DuckDBDataChunk chunk)
    {
        _rowCount = NativeMethods.DataChunks.DuckDBDataChunkGetSize(chunk);
        _column0Vector = TVector1.Create(chunk, 0);
        _column1Vector = TVector2.Create(chunk, 1);
    }

    public void Reset(DuckDBDataChunk chunk)
    {
        _column0Vector.Reset(chunk, 0);
        _column1Vector.Reset(chunk, 1);
    }

    public static bool TryCreate(ref DuckDBResult result, out DataChunk<TVector1, TVector2, T1, T2> chunk)
    {
        var c = NativeMethods.Query.DuckDBFetchChunk(result);
        if (c.IsInvalid)
        {
            chunk = default;
            return false;
        }

        chunk = new DataChunk<TVector1, TVector2, T1, T2>(c);
        return true;
    }

    public bool TryGetRow(int offset, out (T1, T2) row)
    {
        if (offset >= (int)_rowCount || offset < 0)
        {
            row = default;
            return false;
        }

        row = (_column0Vector.GetLowLevel(offset), _column1Vector.GetLowLevel(offset));
        return true;
    }
}

