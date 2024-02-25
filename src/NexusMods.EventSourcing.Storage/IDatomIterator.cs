using System;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage;

public interface IDatomIterator
{
    ref RawDatom Current { get; }

    Memory<byte> Data { get; }

    bool Next();

    bool Prev();
}
