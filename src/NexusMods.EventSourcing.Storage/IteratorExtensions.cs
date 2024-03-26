using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage;

public static class IteratorExtensions
{
    /// <summary>
    /// Gets the KeyPrefix of the current datom
    /// </summary>
    public static KeyPrefix CurrentPrefix(this IDatomIterator iterator)
    {
        return MemoryMarshal.Read<KeyPrefix>(iterator.Current);
    }

    public static IReadDatom Resolve(this IDatomIterator iterator, AttributeRegistry registry)
    {
        var c = iterator.CurrentPrefix();
        return registry.Resolve(c.E, c.A, iterator.Current, c.T, c.IsRetract);
    }

    public static void Seek(this IDatomIterator iterator, EntityId e, AttributeId a, TxId tx, bool isRetract = false)
    {
        var key = new KeyPrefix();
        key.Set(e, a, tx, isRetract);
        iterator.Seek(MemoryMarshal.CreateSpan(ref key, 1).CastFast<KeyPrefix, byte>());
    }

}
