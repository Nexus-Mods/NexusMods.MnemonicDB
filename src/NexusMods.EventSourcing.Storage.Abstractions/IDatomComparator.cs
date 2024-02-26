using System;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public interface IDatomComparator
{
    public int Compare(in Datom a, in Datom b);
}
