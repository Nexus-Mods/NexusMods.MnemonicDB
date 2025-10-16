using System;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Storage;

public partial class DatomStore
{

    /// <summary>
    /// Normalizes the datoms in the input list. Normalization involves the following:
    /// * Retracts are added for any cardinality one updates
    /// * Datoms attached to EntityIds in the temp partition are assumed to not need implicit retracts
    /// * Datoms for cardinaltiy many attributes do not have implied retracts
    /// * Datoms are applied the order they are given to this function
    /// * Set/Retract/Set for the same EntityId/Attribute are reduced down to a single Set
    /// * Set/Set/Set is normaled down to the final Set
    /// * Unique constraints that are violated throw an exception, there is no implied retract for
    ///   unique attributes
    /// * Any retracted datoms (including the TxId) are logged in ToRetract
    /// </summary>
    /// <param name="datoms"></param>
    /// <returns></returns>
    private (DatomList Normalized, DatomList ToRetract) Normalize(ReadOnlySpan<ValueDatom> datoms)
    {
        
    }
    
}
