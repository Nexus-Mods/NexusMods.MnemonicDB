using System.Threading;

namespace NexusMods.MnemonicDB.Abstractions;

public static class TempId
{
    /// <summary>
    /// The next temp id to use, this is global so that all transactiosn can be mixed and combined
    /// during the life of the application. 
    /// </summary>
    private static ulong _counter = PartitionId.Temp.MakeEntityId(1).Value;
    
    /// <summary>
    /// Creates a new temp id.
    /// </summary>
    public static EntityId Next()
    {
        return Next(PartitionId.Entity);
    }
    
    /// <summary>
    /// Creates a new temp id for the given partition.
    /// </summary>
    public static EntityId Next(PartitionId entityPartition)
    {
        // Add the partition to the id
        var actualId = ((ulong)entityPartition << 40) | Interlocked.Increment(ref _counter);
        return EntityId.From(actualId);
    }
}
