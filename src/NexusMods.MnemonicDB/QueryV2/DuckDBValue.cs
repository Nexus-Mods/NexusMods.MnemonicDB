using System;
using System.Runtime.InteropServices;
using DuckDB.NET.Native;

namespace NexusMods.MnemonicDB.QueryV2;

[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public unsafe struct DuckDBValue : IDisposable
{
    [FieldOffset(0)]
    private IntPtr _value;

    public DuckDBValue(IntPtr value)
    {
        _value = value;
    }
    
    public bool IsValid => _value != IntPtr.Zero;
    
    public void Dispose()
    {
        if (!IsValid)
            return;
        NativeMethods.Value.DuckDBDestroyValue(ref _value);
    }
}
