using System.Linq;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.MnemonicDB.Storage;

/// <summary>
/// Next ID cache
/// </summary>
[InlineArray(256)]
public struct NextIdCache
{
    private ulong _nextId;
    

    /// <summary>
    /// Gets the next id for the given partition
    /// </summary>
    public EntityId NextId(ISnapshot snapshot, PartitionId partitionId)
    {
        var partition = partitionId.Value;
        if (this[partition] == 0)
        {
            var lastEnt = LastEntityInPartition(snapshot, partitionId);
            this[partition] = lastEnt.Value;
        }

        var newId = ++this[partition];
        return partitionId.MakeEntityId(newId);
    }
    
    /// <summary>
    /// Gets the last recorded entity in the partition in the snapshot
    /// </summary>
    public EntityId LastEntityInPartition(ISnapshot snapshot, PartitionId partitionId)
    {
        var partition = partitionId.Value;
        if (this[partition] != 0)
        {
            return partitionId.MakeEntityId(this[partition]);
        }

        if (snapshot.TryGetMaxIdInPartition(partitionId, out var id))
        {
            this[partition] = id.Value;
            return id;
        }
        
        // If we don't have a max id, we need to use the min id
        this[partition] = 0;
        return partitionId.MinValue;
    }
}
