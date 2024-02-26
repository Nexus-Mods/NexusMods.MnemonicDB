namespace NexusMods.EventSourcing.Storage.Abstractions;

public class Configuration
{
    public uint IndexBlockSize { get; set; } = 1024 * 2;
    public uint DataBlockSize { get; set; } = 1024 * 8;

    public static Configuration Default { get; } = new();
}
