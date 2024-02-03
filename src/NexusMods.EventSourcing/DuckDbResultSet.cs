using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using DuckDB.NET;

using static DuckDB.NET.NativeMethods.Appender;
using static DuckDB.NET.NativeMethods.DataChunks;
using static DuckDB.NET.NativeMethods.LogicalType;
using static DuckDB.NET.NativeMethods.Query;
using static DuckDB.NET.NativeMethods.Startup;
using static DuckDB.NET.NativeMethods.Types;


namespace NexusMods.EventSourcing;

/// <summary>
/// Contains all the pointers for a datom result set from DuckDB and all the way of accessing the data
/// </summary>
unsafe struct DuckDbResultSet
{
    private const int NumChildren = 8;

    public DuckDBResult Result;

    private fixed IntPtr _childData[NumChildren];
    private fixed IntPtr _childValidity[NumChildren];
    private long _rowCount;
    private long _chunkCount;
    private DuckDBDataChunk _currentChunk;
    private long _chunkSize;
    private int _chunkIndex;
    private IntPtr _eDataVector;
    private ulong* _eData;
    private int _chunkRow;
    private IntPtr _aDataVector;
    private ulong* _aData;

    private IntPtr _txDataVector;
    private ulong* _txData;

    public DuckDbResultSet()
    {
        _currentChunk = new DuckDBDataChunk();
        _chunkIndex = 0;
    }

    /// <summary>
    /// Initializes the result set after Result has been set externally.
    /// </summary>
    public void Init()
    {
        _chunkCount = DuckDBResultChunkCount(Result);

        _currentChunk = DuckDBResultGetChunk(Result, 0);
        _chunkSize = DuckDBDataChunkGetSize(_currentChunk);
        _chunkIndex = 0;
        _chunkRow = 0;
    }

    private void InitChunk()
    {
        _eDataVector = DuckDBDataChunkGetVector(_currentChunk, 0);
        _eData = (ulong*)DuckDBVectorGetData(_eDataVector);

        _aDataVector = DuckDBDataChunkGetVector(_currentChunk, 1);
        _aData = (ulong*)DuckDBVectorGetData(_aDataVector);

        _txDataVector = DuckDBDataChunkGetVector(_currentChunk, 3);
        _txData = (ulong*)DuckDBVectorGetData(_txDataVector);

        // Value Struct Reading

        var vData = DuckDBDataChunkGetVector(_currentChunk, 2);
        for (var childId = 1; childId < NumChildren; childId++)
        {
            var child = DuckDBStructVectorGetChild(vData, childId);
            _childData[childId] = (IntPtr)DuckDBVectorGetData(child);
            _childValidity[childId] = (IntPtr)DuckDBVectorGetValidity(child);
        }
    }

    // Ratchets to the next row in the result set. Returns false if there are no more rows.
    public bool Next()
    {
        if (_chunkRow < _chunkSize)
        {
            _chunkRow++;
            return true;
        }
        else if (_chunkIndex < _chunkCount - 1)
        {
            _chunkIndex++;
            _currentChunk = DuckDBResultGetChunk(Result, _chunkIndex);
            _chunkSize = DuckDBDataChunkGetSize(_currentChunk);
            _chunkRow = 0;
            InitChunk();
            return true;
        }
        else
        {
            return false;
        }
    }




    private ulong EntityId => _eData[_chunkRow];
    private ulong Attribute => _eData[_chunkRow + 1];
    private ulong Tx => _txData[_chunkRow];

    public enum ValueTypes : int
    {
        Unknown = 0,
        Int64 = 1,
        UInt64 = 2,
        String = 3,
        Boolean = 4,
        Double = 5,
        Float = 6,
        Bytes = 7
    }

    public ValueTypes ValueType => (ValueTypes)ValidChild();

    public long ValueInt64 => ((long*)_childData[1])[_chunkRow];
    public ulong ValueUInt64 => ((ulong*)_childData[2])[_chunkRow];
    public string ValueString
    {
        get
        {
            var ptr = (void*)_childData[3];
            var data = (DuckDBString2*)ptr + _chunkRow;
            return new string(data->Data, 0, data->Length, Encoding.UTF8);
        }
    }
    public bool ValueBoolean => ((byte*)_childData[4])[_chunkRow] != 0;
    public double ValueDouble => ((double*)_childData[5])[_chunkRow];
    public float ValueFloat => ((float*)_childData[6])[_chunkRow];


    /// <summary>
    /// Returns the index of the child struct member that is valid for the current row.
    /// </summary>
    /// <returns></returns>
    private unsafe int ValidChild()
    {
        var idx = _chunkRow >> 16;
        var vector = Vector128.Create(0,
            ((ushort*)_childValidity[1])[idx],
            ((ushort*)_childValidity[2])[idx],
            ((ushort*)_childValidity[3])[idx],
            ((ushort*)_childValidity[4])[idx],
            ((ushort*)_childValidity[5])[idx],
            ((ushort*)_childValidity[6])[idx],
            0);

        var shifted = Sse2.ShiftRightLogical(vector, (byte)(_chunkRow % 16));
        shifted &= Vector128<ushort>.One;

        var mask = Sse2.CompareEqual(shifted, Vector128<ushort>.One).AsByte();
        var maskInt = Sse2.MoveMask(mask);

        int index = BitOperations.TrailingZeroCount(maskInt) >> 1;

        return index;
    }


}
