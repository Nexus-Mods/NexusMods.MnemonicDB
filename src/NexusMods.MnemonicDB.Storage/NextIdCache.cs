using System.Linq;
using System.Runtime.CompilerServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;

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

        var startPrefix = new KeyPrefix().Set(partitionId.MakeEntityId(ulong.MaxValue), AttributeId.Min, TxId.MinValue, false);
        var endPrefix = new KeyPrefix().Set(partitionId.MakeEntityId(0), AttributeId.Max, TxId.MaxValue, false);

        var lastEnt = snapshot.Datoms(IndexType.EAVTCurrent, startPrefix, endPrefix)
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
