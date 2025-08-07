using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace NexusMods.HyperDuck.Internals;

public class LiveQueryUpdater : IAsyncDisposable
{
    private readonly TimeSpan _delay;
    private readonly CancellationTokenSource _cancelationToken;
    private readonly ConcurrentDictionary<ulong, ILiveQuery> _liveQueries = [];
    private ImmutableStack<TaskCompletionSource> _pendingFlushes = ImmutableStack<TaskCompletionSource>.Empty;
    private Task? _task;
    

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
        _task = Task.Run(async () =>
        {
            try
            {
                while (!_cancelationToken.IsCancellationRequested)
                {
                    Pulse();
                    await Task.Delay(_delay, _cancelationToken.Token);
                }
            }
            catch (TaskCanceledException)
            {
                // Do Nothing, this is normal
            }
        });
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

    public async ValueTask DisposeAsync()
    {
        if (_task == null) 
            return;
        // It's a bit of deep async lore, but `.CancelAsync` will sometimes run
        // the cancellation callbacks on the caller threads, which can hang the code
        // The CancelAfter variant doesn't have that issue. 
        _cancelationToken.CancelAfter(0); 
        await _task;
        _task = null;
        _liveQueries.Clear();
        _pendingFlushes = ImmutableStack<TaskCompletionSource>.Empty;
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
