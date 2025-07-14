using System;

namespace NexusMods.HyperDuck;

internal static class InternalConsts
{

    #if IS_WINDOWS
    public const string LibraryName = "runtimes/win-x64/native/duckdb.dll";
    #elif IS_LINUX
    public const string LibraryName = "runtimes/linux-x64/native/duckdb.so";
    #elif IS_MACOS
    public const string LibraryName = "runtimes/osx-arm64/native/duckdb.dylib";
    #else
    public const string LibraryName = "NO_KNOWN_DLL_MAPPING FOR THIS OS";
    #endif
    
    
    public const int DefaultVectorSize = 1024 * 2;
    
}
