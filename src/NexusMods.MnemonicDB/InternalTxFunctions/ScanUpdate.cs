using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB.InternalTxFunctions;

internal class ScanUpdate : AInternalFn
{
    private readonly IConnection.ScanFunction _function;

    public ScanUpdate(IConnection.ScanFunction function)
    {
        _function = function;
        
    }
    
    public override void Execute(DatomStore store)
    {
        using var batch = store.Backend.CreateBatch();
        using var writer = new PooledMemoryBufferWriter();

        var changes = false;
        using var iter = store.CurrentSnapshot.LightweightDatoms(SliceDescriptor.All);
        while(iter.MoveNext())
        {
            var prefix = iter.Prefix;
            writer.Reset();
            var datom = Datom.Create(iter);
            var type = _function(ref datom);
            switch (type)
            {
                case ScanResultType.None:
                    break;
                case ScanResultType.Update:
                    batch.Delete(Datom.Create(iter));
                    batch.Add(datom);
                    changes = true;
                    break;
                case ScanResultType.Delete:
                    batch.Delete(Datom.Create(iter));
                    changes = true;
                    break;
            }
        }

        
        if (changes)
        {
            store.LogDatoms(batch, new Datoms(store.AttributeResolver), advanceTx: false);
        }
    }
}
