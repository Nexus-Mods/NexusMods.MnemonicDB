using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Storage;
using static NexusMods.MnemonicDB.Abstractions.IndexType;

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
        // Find all datoms for the given entity ids
        var snapshot = store.CurrentSnapshot;
        using var currentDatomsBuilder = new IndexSegmentBuilder(store.AttributeCache);
        using var historyDatomsBuilder = new IndexSegmentBuilder(store.AttributeCache);
        foreach (var entityId in ids)
        {
            // All Current datoms
            var segment = snapshot.Datoms(SliceDescriptor.Create(entityId));
            currentDatomsBuilder.Add(segment);
            
            // All History datoms
            segment = snapshot.Datoms(SliceDescriptor.Create(EAVTHistory, entityId));
            historyDatomsBuilder.Add(segment);
        }
        
        // Build the datoms
        var currentDatoms = currentDatomsBuilder.Build();
        var historyDatoms = historyDatomsBuilder.Build();
        
        // Start the batch
        var batch = store.Backend.CreateBatch();
        
        // Delete all datoms in the history and current segments
        foreach (var datom in historyDatoms)
        {
            batch.Delete(EAVTHistory, datom);
            batch.Delete(AEVTHistory, datom);
            batch.Delete(VAETHistory, datom);
            batch.Delete(AVETHistory, datom);
            batch.Delete(TxLog, datom);
        }

        foreach (var datom in currentDatoms)
        {
            batch.Delete(EAVTCurrent, datom);
            batch.Delete(AEVTCurrent, datom);
            batch.Delete(VAETCurrent, datom);
            batch.Delete(AVETCurrent, datom);
            batch.Delete(TxLog, datom);
        }
        batch.Commit();

        // Push through a marker transaction to make sure all indexes are updated
        {
            using var builder = new IndexSegmentBuilder(store.AttributeCache);
            var txId = EntityId.From(PartitionId.Temp.MakeEntityId(0).Value);
            builder.Add(txId, Abstractions.BuiltInEntities.Transaction.ExcisedDatoms, (ulong)currentDatoms.Count);
            store.LogDatoms(builder.Build());
        }
    }
}
