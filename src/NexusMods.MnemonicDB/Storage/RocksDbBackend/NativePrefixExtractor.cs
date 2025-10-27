using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using Reloaded.Memory.Pointers;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

public static class NativePrefixExtractor
{
    private const int PrefixSize = 8;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe IntPtr TransformFunc(IntPtr state, IntPtr key, UIntPtr length, UIntPtr* prefixLen)
    {
        *prefixLen = PrefixSize;
        var result = Marshal.AllocHGlobal(PrefixSize);
        var prefix = (*((KeyPrefix*)key));
        var indexType = prefix.Index;

        // All prefix keys are in the format of
        // Index (1 byte)
        // Value (7 bytes)
        
        // Store the index type first
        var outVal = ((ulong)indexType) << 56;
        
        switch (indexType)
        {
            case IndexType.EAVTCurrent:
            case IndexType.EAVTHistory:
                // The E value is already packed in the upper 56 bits of the lower
                outVal |= prefix.Lower >> 8;
                break;
            case IndexType.AEVTCurrent:
            case IndexType.AEVTHistory:
            case IndexType.AVETCurrent:
            case IndexType.AVETHistory:
                // A value is all we need, so we just put that in the lower 16 bits
                outVal |= prefix.A.Value;
                break;
            case IndexType.VAETCurrent:
            case IndexType.VAETHistory:
                // In this case, we need to get the reference id which is stored in the value
                var eid = *((ulong*)(key + KeyPrefix.Size));
                // Now we need to pack it by removing the left-but-one byte from the ulong
                var packed = ((eid & 0xFF00000000000000) >> 8) | (eid & 0x0000FFFFFFFFFFFF);
                outVal |= packed;
                break;
            case IndexType.TxLog:
                outVal |= prefix.Upper & 0x000000FFFFFFFFFF;
                break;
        }
        
        ((ulong*)result)[0] = outVal;
        
        return result;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte InDomain(IntPtr state, IntPtr key, UIntPtr length) => (byte)((int)length >= KeyPrefix.Size ? 1 : 0);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte InRange(IntPtr state, IntPtr key, UIntPtr length) => (byte)((int)length == PrefixSize ? 1 : 0);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void DestructorFunc(IntPtr state) { }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr NameFunc() => Marshal.StringToHGlobalAnsi("ExtractorV1");

    public static unsafe IntPtr MakeSliceTransform()
    {
        delegate* unmanaged[Cdecl]<IntPtr, void> destructorPtr;
        destructorPtr = &DestructorFunc;
        delegate* unmanaged[Cdecl]<IntPtr, IntPtr, UIntPtr, UIntPtr*, IntPtr> transformPtr;
        transformPtr = &TransformFunc;
        delegate* unmanaged[Cdecl]<IntPtr, IntPtr, UIntPtr, byte> inDomainPtr;
        inDomainPtr = &InDomain;
        delegate* unmanaged[Cdecl]<IntPtr, IntPtr, UIntPtr, byte> inRangePtr;
        inRangePtr = &InRange;
        delegate* unmanaged[Cdecl]<IntPtr> namePtr;
        namePtr = &NameFunc;
        
        
        var ptr = Native.Instance.rocksdb_slicetransform_create(
            IntPtr.Zero,
            (IntPtr)destructorPtr,
            (IntPtr)transformPtr,
            (IntPtr)inDomainPtr,
            (IntPtr)inRangePtr,
            (IntPtr)namePtr);
        return ptr;
    }
}
