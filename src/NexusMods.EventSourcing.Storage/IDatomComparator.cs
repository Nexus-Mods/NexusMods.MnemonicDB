namespace NexusMods.EventSourcing.Storage;

public interface IDatomComparator<TDatomA, TDatomB>
    where TDatomA : IRawDatom
    where TDatomB : IRawDatom
{
    public int Compare(in TDatomA x, in TDatomB y);
}
