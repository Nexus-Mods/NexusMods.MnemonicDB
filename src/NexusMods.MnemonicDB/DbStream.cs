using System;
using System.Collections.Immutable;
using System.Reactive;
using System.Threading;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB;

/// <summary>
/// Represents an observable stream of database changes, a IObservable of IDb changes that never goes backwards or repeats transactions
/// </summary>
public class DbStream : IObservable<IDb>, IDisposable
{
    private ImmutableHashSet<IObserver<IDb>> _observers = ImmutableHashSet<IObserver<IDb>>.Empty;
    private IDb? _db;
    
    /// <summary>
    /// Returns the current database value
    /// </summary>
    public IDb Current => _db ?? throw new InvalidOperationException("No current transaction");
    
    /// <summary>
    /// Enqueues the next transaction in the stream
    /// </summary>
    public void OnNext(IDb db)
    {
        _db = db;
        foreach (var observer in _observers)
            observer.OnNext(db);
    }

    /// <summary>
    /// Subscribes to the stream
    /// </summary>
    /// <param name="observer"></param>
    /// <returns></returns>
    public IDisposable Subscribe(IObserver<IDb> observer)
    {
        ImmutableHashSet<IObserver<IDb>> newObservers, oldObservers;
        var forwardOnlyObserver = new ForwardOnlyObserver(observer);
        do 
        {
            oldObservers = _observers;
            newObservers = oldObservers.Add(forwardOnlyObserver);
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref _observers, newObservers, oldObservers), oldObservers));
        
        // Prime the observer with the current transaction
        if (_db is not null)
            forwardOnlyObserver.OnNext(_db);
        
        return new Subscription(this, forwardOnlyObserver);
    }

    /// <summary>
    /// Helper class to manage the subscription
    /// </summary>
    private class Subscription(DbStream stream, ForwardOnlyObserver observer) : IDisposable
    {
        public void Dispose()
        {
            ImmutableHashSet<IObserver<IDb>> newObservers, oldObservers;
            do 
            {
                oldObservers = stream._observers;
                newObservers = oldObservers.Remove(observer);
            } while (!ReferenceEquals(Interlocked.CompareExchange(ref stream._observers, newObservers, oldObservers), oldObservers));
            
            observer.OnCompleted();
        }
    }
    
    /// <summary>
    /// An observer that ensures that we only ever move forward in the transaction stream
    /// </summary>
    private class ForwardOnlyObserver(IObserver<IDb> next) : IObserver<IDb>
    {
        private IDb? _prev;
        
        public void OnCompleted()
        {
            next.OnCompleted();
        }

        public void OnError(Exception error)
        {
            next.OnError(error);
        }

        public void OnNext(IDb value)
        {
            // Common case, this is the next transaction
            if (_prev is not null && _prev.BasisTxId.Value + 1 == value.BasisTxId.Value)
            {
                _prev = value;
                next.OnNext(_prev);
                return;
            }

            // First transaction
            if (_prev is null)
            {
                _prev = value;
                next.OnNext(value);
            }

            // Odd, but somehow we got a transaction behind the previous one
            if (_prev.BasisTxId.Value >= value.BasisTxId.Value)
                return;

            // Otherwise we somehow missed a transaction, so we need to replay the transaction stream
            for (var txId = _prev.BasisTxId.Value + 1; txId < value.BasisTxId.Value; txId++)
            {
                next.OnNext(value.Connection.AsOf(TxId.From(txId)));
            }
            _prev = value;
            next.OnNext(value);
        }
    }


    /// <summary>
    /// Disposes of the stream
    /// </summary>
    public void Dispose()
    {
        foreach (var observer in _observers)
            observer.OnCompleted();
    }

}
