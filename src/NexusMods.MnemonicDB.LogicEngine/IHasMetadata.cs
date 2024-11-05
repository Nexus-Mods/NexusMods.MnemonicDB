using System.Collections.Generic;
using System.Collections.Immutable;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.LogicEngine;

using Metadata = ImmutableDictionary<Symbol, object>;

public interface IHasMetadata
{
    public Metadata Metadata { get; }
    
    /// <summary>
    /// Get metadata by key and cast it to the specified type
    /// </summary>
    public T GetMetadata<T>(Symbol key) => (T)Metadata[key];

    /// <summary>
    /// Tries to get metadata by key and cast it to the specified type
    /// </summary>
    public bool TryGetMetadata<T>(Symbol key, out T value)
    {
        if (Metadata.TryGetValue(key, out var obj) && obj is T t)
        {
            value = t;
            return true;
        }

        value = default!;
        return false;
    }
}
