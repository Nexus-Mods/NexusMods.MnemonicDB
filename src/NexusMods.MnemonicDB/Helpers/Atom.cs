using System;
using System.Threading;

namespace NexusMods.MnemonicDB.Helpers;

/// <summary>
/// A very basic port of Clojure's Atom, a wrapper around a value that can be updated atomically
/// </summary>
public class Atom<T> 
    where T : class
{
    private T _value = default!;
    
    /// <summary>
    /// Constructs a new atom with the given value
    /// </summary>
    public Atom(T value) => _value = value;
    
    /// <summary>
    /// Unconditionally sets the value of the atom
    /// </summary>
    /// <param name="value"></param>
    public void Reset(T value) => _value = value;
    
    /// <summary>
    /// Swaps out the value of the atom using a function that takes the current value and returns the new value, this function
    /// may be run several times if the value is changed by another thread. Returns the new value.
    /// </summary>
    public T Swap<TState>(Func<T, TState, T> f, TState state)
    {
        T original, updated;
        do
        {
            original = _value;
            updated = f(original, state);
        } while (!ReferenceEquals(original, Interlocked.CompareExchange(ref _value, updated, original)));
        return updated;
    }

    /// <summary>
    /// Gets the current value of the atom
    /// </summary>
    public T Value => _value;
}
