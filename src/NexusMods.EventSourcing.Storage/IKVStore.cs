using System;

namespace NexusMods.EventSourcing.Storage;

/// <summary>
/// Implements a interface for loading and saving blocks of data.
/// </summary>
public interface IKvStore
{
    public void Put(UInt128 key, ReadOnlySpan<byte> value);

    public bool TryGet(UInt128 key, out ReadOnlySpan<byte> value);

    public void Delete(UInt128 key);
}
