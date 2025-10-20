using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

/// <summary>
/// This is a lot of boilerplate, but it allows us to serve up raw function pointers to RocksDB instead of managed delegates.
/// This means that .NET can remove a lot of safety checks and marshalling overhead.
/// </summary>
internal static unsafe class NativeComparators
{
    public static readonly IntPtr ComparatorPtr = Native.Instance.rocksdb_comparator_create(IntPtr.Zero, 
        NativeComparators.GetDestructorPtr(),
        NativeComparators.GetNativeFnPtr(),
        NativeComparators.GetNamePtr());
    
    public static IntPtr GetNativeFnPtr()
    {
        delegate* unmanaged[Cdecl]<byte*, byte*, int, byte*, int, int> ptr;
        ptr = &GlobalCompare;
        return (IntPtr) ptr;
    }
    
    public static IntPtr GetDestructorPtr()
    {
        delegate* unmanaged[Cdecl]<IntPtr, void> ptr;
        ptr = &Destructor;
        return (IntPtr) ptr;
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Destructor(IntPtr state)
    {
        // Do nothing
    }

    public static IntPtr GetNamePtr()
    {
        delegate* unmanaged[Cdecl]<IntPtr, IntPtr> ptr;
        ptr = &GlobalCompareName;
        return (IntPtr) ptr;
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr GlobalCompareName(IntPtr state) 
        => Marshal.StringToHGlobalAnsi("GlobalCompare");
    
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int GlobalCompare(byte* state, byte* aPtr, int aLen, byte* bPtr, int bLen) 
        => GlobalComparer.Compare(aPtr, aLen, bPtr, bLen);
}
    
