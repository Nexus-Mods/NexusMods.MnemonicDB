using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.Cascade;
using NexusMods.Cascade.Collections;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;

namespace NexusMods.MnemonicDB.Abstractions.Cascade.Flows;

public class LatestTxForEntity : Flow<(EntityId Entity, EntityId Tx)>
{
    public LatestTxForEntity(Flow<IDb> upstream)
    {
        Upstream = [upstream];
    }
    
    public override Node CreateNode(Topology topology)
    {
        return new LatestTxForEntityNode(topology, this);
    }
    
    private class LatestTxForEntityNode(Topology topology, LatestTxForEntity flow) : Node<(EntityId Entity, EntityId Tx)>(topology, flow, 1)
    {
        private IDb? _db;
        private Dictionary<EntityId, EntityId> _latestTx = new();
        private Dictionary<EntityId, EntityId> _toEmit = new();
        
        public override void Accept<TIn>(int idx, IToDiffSpan<TIn> diffs)
        {
            var span = ((IToDiffSpan<IDb>)diffs).ToDiffSpan();
            var update = Cascade.Query.ToDbUpdate(span);

            TOP:
            switch (update.UpdateType)
            {
                case UpdateType.None:
                    return;
                case UpdateType.Init:
                {
                    using var datoms = update.Next!.LightweightDatoms(SliceDescriptor.Create(IndexType.EAVTCurrent), true);
                    var currentEId = EntityId.From(0);
                    // We have to assign it to something, and intially it will be 0
                    ref var currentSlot = ref currentEId;
                    while (datoms.MoveNext())
                    {
                        var prefix = datoms.KeyPrefix;
                        if (prefix.E.InPartition(PartitionId.Transactions))
                            continue;
                        var txEntity = EntityId.From(prefix.T.Value);

                        if (prefix.E != currentEId)
                        {
                            currentSlot = ref CollectionsMarshal.GetValueRefOrAddDefault(_latestTx, prefix.E, out _);
                        }
                        currentSlot = txEntity > currentSlot ? txEntity : currentSlot;
                    }

                    foreach (var (entityId, txId) in _latestTx)
                    {
                        Output.Add((entityId, txId), 1);
                    }

                    break;
                }
                case UpdateType.NextTx:
                {
                    _toEmit.Clear();
                    
                    foreach (var datom in update.Next!.RecentlyAdded)
                    {
                        if (datom.E.InPartition(PartitionId.Transactions))
                            continue;
                        ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(_latestTx, datom.E, out var added);
                        var txEntity = EntityId.From(datom.T.Value);
                        if (txEntity > slot)
                        {
                            _toEmit.TryAdd(datom.E, slot);
                            slot = txEntity;
                        }
                    }

                    foreach (var (e, oldTx) in _toEmit)
                    {
                        Output.Add((e, oldTx), -1);
                        Output.Add((e, _latestTx[e]), 1);
                    }
                    
                    _toEmit.Clear();
                    break;
                }
                case UpdateType.RemoveAndAdd:
                {
                    foreach (var (e, tx) in _latestTx)
                    {
                        Output.Add((e, tx), -1);
                    }
                    _latestTx.Clear();
                    
                    update = new DbUpdate(null, update.Next, UpdateType.Init);
                    goto TOP;
                }
            }
        }

        public override void Created()
        {
            var upstream = (Node<IDb>)Upstream[0];
            upstream.Output.Clear();
            upstream.Prime();
            Accept(0, upstream.Output);
            upstream.Output.Clear();
        }

        public override void Prime()
        {
            foreach (var (entityId, txId) in _latestTx)
            {
                Output.Add((entityId, txId), 1);
            }
        }
    }

}
