using System;
using System.Threading;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Helpers;

/// <summary>
/// An atom that holds a transaction id
/// </summary>
public class TxIdAtom
{
    private ulong _value;
    
    /// <summary>
    /// Atomically swaps the value of the atom using a function that takes the current value (and state) and returns the new value, this function
    /// </summary>
    public TxId Swap<TState>(Func<TxId, TState, TxId> f, TState state)
    {
        ulong original, updated;
        do
        {
            original = _value;
            updated = f(TxId.From(original), state).Value;
        } while (Interlocked.CompareExchange(ref _value, updated, original) == original);
        return TxId.From(updated);
    }
    
    /// <summary>
    /// Returns the current value of the atom
    /// </summary>
    public TxId Value => TxId.From(_value);
    
    
}
