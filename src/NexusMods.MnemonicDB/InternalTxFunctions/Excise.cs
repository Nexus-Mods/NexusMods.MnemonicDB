using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB.InternalTxFunctions;

/// <summary>
/// Deletes the entities and all associated datoms from the database including
/// the datoms in the history and tx log index.
/// </summary>
/// <param name="ids"></param>
internal class Excise(EntityId[]  ids) : AInternalFn
{
    public override void Execute(DatomStore store)
    {
        // Retract all datoms for the given entity ids
        var snapshot = store.GetSnapshot();

        using IndexSegmentBuilder currentDatomsBuilder = new IndexSegmentBuilder(store.AttributeCache);
        using IndexSegmentBuilder historyDatomsBuilder = new IndexSegmentBuilder(store.AttributeCache);
        foreach (var entityId in ids)
        {
            // All Current datoms
            var segment = snapshot.Datoms(SliceDescriptor.Create(entityId));
            currentDatomsBuilder.Add(segment);
            
            // All History datoms
            segment = snapshot.Datoms(SliceDescriptor.Create(IndexType.EAVTHistory, entityId));
            historyDatomsBuilder.Add(segment);
        }
        
        var currentDatoms = currentDatomsBuilder.Build();
        var historyDatoms = historyDatomsBuilder.Build();
        
        var batch = store.Backend.CreateBatch();
        foreach (var datom in historyDatoms)
        {
            store.EAVTHistory.Delete(batch, datom);
            store.AEVTHistory.Delete(batch, datom);
            store.VAETHistory.Delete(batch, datom);
            store.AVETHistory.Delete(batch, datom);
            store.TxLogIndex.Delete(batch, datom);
        }

        foreach (var datom in currentDatoms)
        {
            store.EAVTCurrent.Delete(batch, datom);
            store.AEVTCurrent.Delete(batch, datom);
            store.VAETCurrent.Delete(batch, datom);
            store.AVETCurrent.Delete(batch, datom);
            store.TxLogIndex.Delete(batch, datom);
        }
        batch.Commit();

        {
            using var builder = new IndexSegmentBuilder(store.AttributeCache);
            var txId = EntityId.From(PartitionId.Temp.MakeEntityId(0).Value);
            builder.Add(txId, Abstractions.BuiltInEntities.Transaction.ExcisedDatoms, (ulong)currentDatoms.Count);
            store.LogDatoms(builder.Build());
        }
    }
}
