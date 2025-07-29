using System;
using System.Security.AccessControl;

namespace NexusMods.HyperDuck;

public static class GlobalConstants
{

    #if IS_WINDOWS
    public const string LibraryName = "runtimes/win-x64/native/duckdb.dll";
    #elif IS_LINUX
    public const string LibraryName = "runtimes/linux-x64/native/duckdb.so";
    #elif IS_MACOS
    public const string LibraryName = "runtimes/osx-arm64/native/libduckdb.dylib";
    #else
    public const string LibraryName = "NO_KNOWN_DLL_MAPPING FOR THIS OS";
    #endif


    public static ulong DefaultVectorSize { get; } = ReadOnlyVector.Native.duckdb_vector_size();

}
