using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Abstractions.Internals;
using Reloaded.Memory.Extensions;

namespace NexusMods.MneumonicDB.Abstractions;

/// <summary>
///     Represents a snapshot of the database at a specific point of time. Snapshots are immutable
///     and do not live past the life of the application, or after the IDisposable.Dispose method is called.
///     Using snapshots to query the database is the most efficient way, and is leveraged by the IDb interface,
///     to provide a read-only view of the database.
/// </summary>
public interface ISnapshot
{
    /// <summary>
    /// Get an enumerable of all the datoms between the given keys, if the keys are in reverse order,
    /// the datoms will be returned in reverse order.
    /// </summary>
    IEnumerable<Datom> Datoms(IndexType type, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);

    /// <summary>
    /// Get an enumerable of all the datoms between the given keys.
    /// </summary>
    IEnumerable<Datom> Datoms(IndexType type, KeyPrefix a, KeyPrefix b)
    {
        return Datoms(type, MemoryMarshal.CreateSpan(ref a, 1).CastFast<KeyPrefix, byte>(),
            MemoryMarshal.CreateSpan(ref b, 1).CastFast<KeyPrefix, byte>());
    }

    /// <summary>
    /// Get an enumerable of all the datoms in the given index.
    /// </summary>
    IEnumerable<Datom> Datoms(IndexType type)
    {
        if (type == IndexType.VAETCurrent || type == IndexType.VAETHistory)
        {
            unsafe
            {
                // We need to pad the key in case this is used in a VAET index that sorts by value first,
                // which would always be a EntityId (ulong)
                Span<byte> a = stackalloc byte[KeyPrefix.Size + sizeof(ulong)];
                a.Clear();
                MemoryMarshal.Write(a, KeyPrefix.Min);

                Span<byte> b = stackalloc byte[KeyPrefix.Size + sizeof(ulong)];
                b.Fill(0xFF);
                MemoryMarshal.Write(b, KeyPrefix.Max);

                return Datoms(type, a, b);
            }
        }

        return Datoms(type, KeyPrefix.Min, KeyPrefix.Max);
    }
}
