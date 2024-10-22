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
    public static IntPtr GetNativeFnPtr()
    {
        var name = nameof(GlobalCompare);
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

    public static IntPtr GetNamePtr()
    {
        var name = nameof(GlobalCompareName);
        return typeof(NativeComparators).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer();
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr GlobalCompareName(IntPtr state) 
        => Marshal.StringToHGlobalAnsi("GlobalCompare");
    
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int GlobalCompare(byte* state, byte* aPtr, int aLen, byte* bPtr, int bLen) 
        => GlobalComparer.Compare(aPtr, aLen, bPtr, bLen);
}
    
