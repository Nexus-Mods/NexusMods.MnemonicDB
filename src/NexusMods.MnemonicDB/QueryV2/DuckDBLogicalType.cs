using System;
using System.Runtime.InteropServices;
using DuckDB.NET.Native;

namespace NexusMods.MnemonicDB.QueryV2;

[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public unsafe struct DuckDBLogicalType : IDisposable
{
    [FieldOffset(0)]
    private IntPtr _value;

    public DuckDBLogicalType(IntPtr value)
    {
        _value = value;
    }
    
    public bool IsValid => _value != IntPtr.Zero;
    
    public void Dispose()
    {
        if (!IsValid)
            return;
        NativeMethods.LogicalType.DuckDBDestroyLogicalType(ref _value);
    }
}
