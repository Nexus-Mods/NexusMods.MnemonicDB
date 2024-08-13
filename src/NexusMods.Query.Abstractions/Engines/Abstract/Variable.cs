using System;
using System.Threading;

namespace NexusMods.Query.Abstractions.Engines.Abstract;

internal static class IdGenerator
{
    private static ulong _id = 0;

    public static ulong NextId() => Interlocked.Increment(ref _id);
}

/// <summary>
/// A variable that will be populated by the engine as it executes
/// </summary>
public record Variable<T>(string Name, ulong Id) : IVariable
{
    public static Variable<T> New(string name = "")
    {
        return new Variable<T>(name, IdGenerator.NextId());
    }

    public override string ToString()
    {
        return $"?{Name}<{typeof(T).Name}>({Id})";
    }

    public Type Type => typeof(T);
}
