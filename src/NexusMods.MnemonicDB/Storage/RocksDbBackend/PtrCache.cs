using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

/// <summary>
/// A small chunk of off-heap memory used to defensively copy the value of a pointer
/// </summary>
public unsafe struct PtrCache : IDisposable
{
    private byte* _rawData;
    private int _fullSize;
    private int _usedSize;
    
    
    /// <summary>
    /// Get the Ptr for this cache
    /// </summary>
    public Ptr Ptr => new(_rawData, _usedSize);

    /// <summary>
    /// Cop
    /// </summary>
    /// <param name="ptr"></param>
    public void CopyFrom(Ptr ptr)
    {
        if (_fullSize < ptr.Length)
        {
            if (_rawData != null)
                Marshal.FreeHGlobal((IntPtr)_rawData);
            _rawData = (byte*)Marshal.AllocHGlobal(ptr.Length);
            _fullSize = ptr.Length;
        }
        ptr.Span.CopyTo(new Span<byte>(_rawData, _usedSize));
        _usedSize = ptr.Length;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_rawData != null)
        {
            Marshal.FreeHGlobal((IntPtr)_rawData);
            _rawData = null;
        }
        _fullSize = 0;
        _usedSize = 0;
    }
}
