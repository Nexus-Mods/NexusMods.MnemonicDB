using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

/// <summary>
/// This is a lot of boilerplate, but it allows us to serve up raw function pointers to RocksDB instead of managed delegates.
/// This means that .NET can remove a lot of safety checks and marshalling overhead.
/// </summary>
internal static unsafe class NativeComparators
{
    public static IntPtr GetNativeFnPtr(IndexType type)
    {
        var name = type switch
        {
            IndexType.EAVTCurrent => nameof(EAVTCompare),
            IndexType.EAVTHistory => nameof(EAVTCompare),
            IndexType.AEVTCurrent => nameof(AEVTCompare),
            IndexType.AEVTHistory => nameof(AEVTCompare),
            IndexType.AVETCurrent => nameof(AVETCompare),
            IndexType.AVETHistory => nameof(AVETCompare),
            IndexType.VAETCurrent => nameof(VAETCompare),
            IndexType.VAETHistory => nameof(VAETCompare),
            IndexType.TxLog => nameof(TxLogCompare),
        };
        return typeof(NativeComparators).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer();
    }
    
    public static IntPtr GetDestructorPtr()
    {
        return typeof(NativeComparators).GetMethod(nameof(Destructor), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer();
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Destructor(IntPtr state)
    {
        // Do nothing
    }

    public static IntPtr GetNamePtr(IndexType type)
    {
        var name = type switch
        {
            IndexType.EAVTCurrent => nameof(EAVTCurrentName),
            IndexType.EAVTHistory => nameof(EAVTHistoryName),
            IndexType.AEVTCurrent => nameof(AEVTCurrentName),
            IndexType.AEVTHistory => nameof(AEVTHistoryName),
            IndexType.AVETCurrent => nameof(AVETCurrentName),
            IndexType.AVETHistory => nameof(AVETHistoryName),
            IndexType.VAETCurrent => nameof(VAETCurrentName),
            IndexType.VAETHistory => nameof(VAETHistoryName),
            IndexType.TxLog => nameof(TxLogName),
        };
        return typeof(NativeComparators).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer();
    }
    #region NameFns

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr EAVTCurrentName(IntPtr state) => Marshal.StringToHGlobalAnsi("EAVTCurrent");
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr EAVTHistoryName(IntPtr state) => Marshal.StringToHGlobalAnsi("EAVTHistory");
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr AEVTCurrentName(IntPtr state) => Marshal.StringToHGlobalAnsi("AEVTCurrent");
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr AEVTHistoryName(IntPtr state) => Marshal.StringToHGlobalAnsi("AEVTHistory");
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr AVETCurrentName(IntPtr state) => Marshal.StringToHGlobalAnsi("AVETCurrent");
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr AVETHistoryName(IntPtr state) => Marshal.StringToHGlobalAnsi("AVETHistory");
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr VAETCurrentName(IntPtr state) => Marshal.StringToHGlobalAnsi("VAETCurrent");
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr VAETHistoryName(IntPtr state) => Marshal.StringToHGlobalAnsi("VAETHistory");
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr TxLogName(IntPtr state) => Marshal.StringToHGlobalAnsi("TxLog");
    
    #endregion

    /// <summary>
    /// Native only method to compare two EAVT keys
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int EAVTCompare(byte* state, byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return EAVTComparator.Compare(aPtr, aLen, bPtr, bLen);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int AEVTCompare(byte* state, byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return AEVTComparator.Compare(aPtr, aLen, bPtr, bLen);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int VAETCompare(byte* state, byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return VAETComparator.Compare(aPtr, aLen, bPtr, bLen);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int AVETCompare(byte* state, byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return AVETComparator.Compare(aPtr, aLen, bPtr, bLen);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int TxLogCompare(byte* state, byte* aPtr, int aLen, byte* bPtr, int bLen)
    {
        return TxLogComparator.Compare(aPtr, aLen, bPtr, bLen);
    }
}
    
