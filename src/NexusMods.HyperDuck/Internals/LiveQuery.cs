using System;
using System.Collections.Generic;
using System.Threading;
using NexusMods.Hashing.xxHash3;
using NexusMods.HyperDuck.Adaptor;
using Reloaded.Memory.Extensions;

namespace NexusMods.HyperDuck.Internals;

public interface ILiveQuery : IDisposable
{
    public void Update();
    
    public ulong Id { get; }

    private static ulong _nextId = 0;
    public static ulong NextId() => Interlocked.Increment(ref _nextId);
}

public class LiveQuery<T> : ILiveQuery
{
    public ulong Id { get; } = ILiveQuery.NextId();
    
    private Hash _lastHash = Hash.Zero;
    
    public required ATableFunction[] DependsOn { get; init; }
    public required LiveQueryUpdater Updater { get; init; }
    public required Connection Connection { get; init; }
    public required string Query { get; init; }
    public required T Output;
    public required IResultAdaptor<T> ResultAdaptor { get; init; }
    private bool _isDisposed = false;
    
    public void Update()
    {
        if (_isDisposed)
            return;
        
        Span<ulong> ids = stackalloc ulong[DependsOn.Length];
        for (var i = 0; i < DependsOn.Length; i++)
        {
            ids[i] = DependsOn[i].Revision;
        }


        var hash = ids.CastFast<ulong, byte>().xxHash3();
        if (hash == _lastHash)
            return;
        
        using var result = Connection.Query(Query);
        ResultAdaptor.Adapt(result, ref Output);
        _lastHash = hash;
    }


    public void Dispose()
    {
        _isDisposed = true;
        Updater.Remove(this);
    }
}
