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
        private Dictionary<EntityId, (EntityId LatestTx, int DatomCount)> _latestTx = new();
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
                    
                    // Allocate a reference on the stack to the tuple, and initially set it to the current entity and count.
                    var storageInit = (Tx: EntityId.From(0), Count:0);
                    ref var currentSlot = ref storageInit;
                    
                    while (datoms.MoveNext())
                    {
                        var prefix = datoms.KeyPrefix;
                        if (prefix.E.InPartition(PartitionId.Transactions))
                            continue;
                        var txEntity = EntityId.From(prefix.T.Value);

                        if (prefix.E != currentEId)
                        {
                            // On the next entity, so update our temporary storage.
                            currentSlot = ref CollectionsMarshal.GetValueRefOrAddDefault(_latestTx, prefix.E, out _);
                            currentEId = prefix.E;
                        }

                        currentSlot.Tx = txEntity;
                        currentSlot.Count++;
                    }

                    foreach (var (entityId, state) in _latestTx)
                    {
                        Output.Add((entityId, state.LatestTx), 1);
                    }

                    break;
                }
                case UpdateType.NextTx:
                {
                    _toEmit.Clear();
                    
                    var newTx = EntityId.From(update.Next!.BasisTxId.Value);
                    
                    foreach (var datom in update.Next!.RecentlyAdded)
                    {
                        if (datom.E.InPartition(PartitionId.Transactions))
                            continue;
                        ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(_latestTx, datom.E, out var added);
                        var txEntity = EntityId.From(datom.T.Value);
                        if (txEntity > slot.LatestTx)
                        {
                            // Do try/add because we want to set the value to the old transaction entity, and we don't want to overwrite it if it already exists.
                            _toEmit.TryAdd(datom.E, slot.LatestTx);
                            slot.LatestTx = newTx;
                        }
                        slot.DatomCount += (datom.Prefix.IsRetract ? -1 : 1);
                    }

                    foreach (var (e, oldTx) in _toEmit)
                    {
                        var count = _latestTx[e].DatomCount;
                        
                        // No datoms left for this entity, so we completely remove the entry
                        if (count == 0)
                        {
                            _latestTx.Remove(e);
                            Output.Add((e, oldTx), -1);
                        }
                        else if (oldTx.Value == 0)
                        {
                            Output.Add((e, newTx), 1);
                        }
                        else
                        {
                            Output.Add((e, oldTx), -1);
                            Output.Add((e, newTx), 1);
                        }
                    }
                    
                    _toEmit.Clear();
                    break;
                }
                case UpdateType.RemoveAndAdd:
                {
                    foreach (var (e, slot) in _latestTx)
                    {
                        Output.Add((e, slot.LatestTx), -1);
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
                Output.Add((entityId, txId.LatestTx), 1);
            }
        }
    }

}
