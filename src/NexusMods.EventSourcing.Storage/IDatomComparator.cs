using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage;

public interface IDatomComparator
{
    public int Compare<TDatomA, TDatomB>(in TDatomA x, in TDatomB y)
    where TDatomA : IRawDatom
    where TDatomB : IRawDatom;
}
