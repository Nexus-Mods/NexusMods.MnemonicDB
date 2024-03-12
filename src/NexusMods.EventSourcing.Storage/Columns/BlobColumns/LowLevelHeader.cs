namespace NexusMods.EventSourcing.Storage.Columns.BlobColumns;

public unsafe struct LowLevelHeader
{
    public uint Count;
    public uint LengthsOffset;
    public uint DataOffset;

}
