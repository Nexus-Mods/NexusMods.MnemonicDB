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
    /// Gets the most recent transaction id
    /// </summary>
    public TxId AsOfTxId => TxId.From(this[(byte)PartitionId.Transactions]);

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

        var descriptor = SliceDescriptor.Create(partitionId.MakeEntityId(ulong.MaxValue), partitionId.MakeEntityId(0));

        var lastEnt = snapshot.DatomsChunked(descriptor, 1)
            .SelectMany(c => c)
            .Select(d => d.E)
            .FirstOrDefault(partitionId.MakeEntityId(0));

        if (lastEnt.Partition != partitionId)
        {
            return partitionId.MakeEntityId(0);
        }
        else
        {
            // Implicitly cache the last entity id
            this[partition] = lastEnt.Value;
            return lastEnt;
        }
    }
}
