namespace NexusMods.EventSourcing.Storage;

public class Configuration
{
    public uint IndexBlockSize { get; set; } = 1024 * 8;
    public uint DataBlockSize { get; set; } = 1024 * 8;
}
