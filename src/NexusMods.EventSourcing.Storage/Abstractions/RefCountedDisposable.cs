using System;
using System.Threading;

namespace NexusMods.EventSourcing.Storage.Abstractions;

/// <summary>
/// A ref counted disposable wrapper around a disposable object. Most often used to track the
/// lifetime of a snapshot
/// </summary>
public class RefCountedDisposable<T>(T inner) : IDisposable where T : IDisposable
{
    private int _refCount;

    public T Inner => inner;

    public IDisposable AddRef()
    {
        Interlocked.Increment(ref _refCount);
        return this;
    }

    public void Dispose()
    {
        if (Interlocked.Decrement(ref _refCount) == 0)
        {
            inner.Dispose();
        }
    }
}
