using System;
using System.Security.AccessControl;

namespace NexusMods.HyperDuck;

public static class GlobalConstants
{

    public const string LibraryName = "duckdb";
    public static ulong DefaultVectorSize { get; } = ReadOnlyVector.Native.duckdb_vector_size();

}
