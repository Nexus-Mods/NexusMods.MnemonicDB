using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

public struct HistoryRefDatomEnumerator<THistory, TCurrent> : IRefDatomEnumerator 
    where THistory : IRefDatomEnumerator 
    where TCurrent : IRefDatomEnumerator
{
    private enum UseSide : byte
    {
        History = 1,
        Current = 2
    }
    
    private readonly THistory _history;
    private readonly TCurrent _current;

    // These fields track whether each enumerator currently has a valid element.
    private bool _historyHasCurrent;
    private bool _currentHasCurrent;
    private bool _initialized;
    
    /// <summary>
    /// If true, values will be pulled from the history enumerator; if false, from the current enumerator.
    /// </summary>
    private UseSide _useSide = UseSide.History;

    public HistoryRefDatomEnumerator(THistory history,
                                    TCurrent current)
    {
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _current = current ?? throw new ArgumentNullException(nameof(current));
        _initialized = false;
        _historyHasCurrent = false;
        _currentHasCurrent = false;
    }


    /// <inheritdoc />
    public KeyPrefix KeyPrefix => _useSide == UseSide.History ? _history.KeyPrefix : _current.KeyPrefix;

    /// <inheritdoc />
    public Ptr Current => _useSide == UseSide.History ? _history.Current : _current.Current;

    /// <inheritdoc />
    public Ptr ValueSpan => _useSide == UseSide.History ? _history.ValueSpan : _current.ValueSpan;

    /// <inheritdoc />
    public Ptr ExtraValueSpan => _useSide == UseSide.History ? _history.ExtraValueSpan : _current.ExtraValueSpan;

    /// <summary>
    /// Advances the merged enumerator to the next datom by choosing the next element from either the history
    /// or current enumerator based on the comparison function.
    /// </summary>
    /// <typeparam name="TSliceDescriptor">A slice descriptor type.</typeparam>
    /// <param name="descriptor">The descriptor used for slicing the data.</param>
    /// <param name="useHistory">
    /// Ignored in this merged implementation; the history enumerator is always advanced using its own flag.
    /// </param>
    /// <returns>True if an element is available; false otherwise.</returns>
    public bool MoveNext<TSliceDescriptor>(TSliceDescriptor descriptor, bool useHistory = false)
        where TSliceDescriptor : ISliceDescriptor, allows ref struct
    {
        // On the first call, "prime" both enumerators.
        if (!_initialized)
        {
            _historyHasCurrent = _history.MoveNext(descriptor, true);
            _currentHasCurrent = _current.MoveNext(descriptor, false);
            _initialized = true;
        }

        // If both enumerators are exhausted, we're done.
        if (!_historyHasCurrent && !_currentHasCurrent)
            return false;

        // If one enumerator is exhausted, return the element from the other.
        if (!_historyHasCurrent)
        {
            _useSide = UseSide.Current;
            _currentHasCurrent = _current.MoveNext(descriptor, false);
            return true;
        }

        if (!_currentHasCurrent)
        {
            _useSide = UseSide.History;
            _historyHasCurrent = _history.MoveNext(descriptor, true);
            return true;
        }

        // Both enumerators have a current element.
        // Compare them using the provided comparison function.
        var cmp = GlobalComparer.Compare(_history.Current, _current.Current);
        if (cmp < 0)
        {
            _useSide = UseSide.History;
            _historyHasCurrent = _history.MoveNext(descriptor, true);
        }
        else // cmp > 0 since 0 is never returned
        {
            _useSide = UseSide.Current;
            _currentHasCurrent = _current.MoveNext(descriptor, false);
        }
        return true;
    }
    

    /// <inheritdoc />
    public void Dispose()
    {
        _history.Dispose();
        _current.Dispose();
    }
}
