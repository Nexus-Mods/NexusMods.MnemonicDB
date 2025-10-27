using System;
using NexusMods.MnemonicDB.Abstractions;
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
        var currentDatoms = new Datoms(store.AttributeCache);
        var historyDatoms = new Datoms(store.AttributeCache);
        foreach (var entityId in ids)
        {
            // All Current datoms
            var segment = snapshot[entityId];
            currentDatoms.Add(segment);
            
            // All History datoms
            segment = snapshot.Datoms(SliceDescriptor.Create(entityId).History());
            historyDatoms.Add(segment);
        }

        // Start the batch
        var batch = store.Backend.CreateBatch();
        
        // Delete all datoms in the history and current segments
        foreach (var datom in historyDatoms)
        {
            batch.Delete(datom.With(EAVTHistory));
            batch.Delete(datom.With(AEVTHistory));
            batch.Delete(datom.With(VAETHistory));
            batch.Delete(datom.With(AEVTHistory));
            batch.Delete(datom.With(TxLog));
        }

        foreach (var datom in currentDatoms)
        {
            batch.Delete(datom.With(EAVTCurrent));
            batch.Delete(datom.With(AEVTCurrent));
            batch.Delete(datom.With(VAETCurrent));
            batch.Delete(datom.With(AVETCurrent));
            batch.Delete(datom.With(TxLog));
        }
        batch.Commit();

        // Push through a marker transaction to make sure all indexes are updated
        {
            var txId = EntityId.From(PartitionId.Temp.MakeEntityId(0).Value);
            var datoms = new Datoms(store.AttributeCache)
            {
                { txId, Abstractions.BuiltInEntities.Transaction.ExcisedDatoms, (ulong)currentDatoms.Count }
            };
            store.LogDatoms(datoms);
        }
    }
}
