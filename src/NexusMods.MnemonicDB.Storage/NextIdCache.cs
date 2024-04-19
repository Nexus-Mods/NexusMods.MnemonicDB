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
    public TxId AsOfTxId => TxId.From(this[(byte)Ids.Partition.Tx]);

    /// <summary>
    /// Gets the next id for the given partition
    /// </summary>
    public EntityId NextId(ISnapshot snapshot, byte partition)
    {
        if (this[partition] == 0)
        {
            var lastEnt = LastEntityInPartition(snapshot, partition);
            this[partition] = lastEnt.Value;
        }

        var newId = ++this[partition];
        return EntityId.From(Ids.MakeId(partition, newId));
    }

    /// <summary>
    /// Gets the current id for the given partition
    /// </summary>
    /// <param name="partition"></param>
    /// <returns></returns>
    public EntityId CurrentId(byte partition)
    {
        return EntityId.From(Ids.MakeId(partition, this[partition]));
    }

    /// <summary>
    /// Gets the last recorded entity in the partition in the snapshot
    /// </summary>
    public EntityId LastEntityInPartition(ISnapshot snapshot, byte partitionId)
    {
        if (this[partitionId] != 0)
        {
            return EntityId.From(Ids.MakeId(partitionId, this[partitionId]));
        }

        var startPrefix = new KeyPrefix().Set(EntityId.From(Ids.MakeId(partitionId, ulong.MaxValue)), AttributeId.Min, TxId.MinValue, false);
        var endPrefix = new KeyPrefix().Set(EntityId.From(Ids.MakeId(partitionId, 0)), AttributeId.Max, TxId.MaxValue, false);

        var lastEnt = snapshot.Datoms(IndexType.EAVTCurrent, startPrefix, endPrefix)
            .Select(d => d.E)
            .FirstOrDefault(EntityId.From(Ids.MakeId(partitionId, 0)));

        if (Ids.GetPartitionValue(lastEnt) != partitionId)
        {
            return EntityId.From(Ids.MakeId(partitionId, 0));
        }
        else
        {
            // Implicitly cache the last entity id
            this[partitionId] = lastEnt.Value;
            return lastEnt;
        }
    }
}
