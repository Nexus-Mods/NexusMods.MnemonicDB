namespace NexusMods.EventSourcing.Abstractions;

public interface IIndex
{
    public void Add(IWriteBatch batch, IWriteDatom datom);
}
