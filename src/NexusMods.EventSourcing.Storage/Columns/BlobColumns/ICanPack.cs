using NexusMods.EventSourcing.Storage.Columns.ULongColumns;

namespace NexusMods.EventSourcing.Storage.Columns.BlobColumns;

public interface ICanPack : IUnpacked
{

    public IReadable Pack()
    {
        var offsets = Offsets.Pack();
        var lengths = Lengths.Pack();

        return null!;
    }

}
