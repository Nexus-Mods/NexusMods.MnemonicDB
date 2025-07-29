using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace NexusMods.HyperDuck.Internals;

public class LiveQueryUpdater : IDisposable
{
    private readonly TimeSpan _delay;
    private readonly CancellationTokenSource _cancelationToken;
    private readonly ConcurrentDictionary<ulong, ILiveQuery> _liveQueries = [];
    private ImmutableStack<TaskCompletionSource> _pendingFlushes = ImmutableStack<TaskCompletionSource>.Empty;
    private Thread? _thread;
    

    public LiveQueryUpdater()
    {
        _delay = TimeSpan.FromSeconds(0.25);
        _cancelationToken = new CancellationTokenSource();
        StartUpdater();
    }

    public void Add(ILiveQuery liveQuery)
    {
        _liveQueries.TryAdd(liveQuery.Id, liveQuery);
    }
    
    public void StartUpdater()
    {
        _thread = new Thread(() =>
        {
            while (!_cancelationToken.IsCancellationRequested)
            {
                Pulse();
                Thread.Sleep(_delay);
            }

        })
        {
            Name = "LiveQueryUpdater",
            IsBackground = true
        };
        _thread.Start();
    }

    private void Pulse()
    {
        var flushes = Interlocked.Exchange(ref _pendingFlushes, ImmutableStack<TaskCompletionSource>.Empty);
        foreach (var liveQuery in _liveQueries.Values)
        {
            liveQuery.Update();
        }
        // The adaptor will mutate the collections it targets, which may
        // be on other threads or have parts of their data cached in various
        // processor cores. So this MemoryBarrier will flush the data written
        // out of those caches. 
        Thread.MemoryBarrier();
        foreach (var flush in flushes)
            flush.SetResult();
    }

    public void Dispose()
    {
        _cancelationToken.Cancel();
    }

    public void Remove(ILiveQuery liveQuery)
    {
        _liveQueries.TryRemove(liveQuery.Id, out _);
    }

    public Task FlushAsync()
    {
        var tcs = new TaskCompletionSource();
        while (true)
        {
            var oldFlushes = _pendingFlushes;
            var newFlushes = oldFlushes.Push(tcs);
            if (ReferenceEquals(Interlocked.CompareExchange(ref _pendingFlushes, newFlushes, oldFlushes), oldFlushes))
                break;
        }
        return tcs.Task;
    }
}
