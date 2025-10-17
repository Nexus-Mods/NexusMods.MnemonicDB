using System;
using NexusMods.MnemonicDB.Abstractions;
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
        throw new NotImplementedException();
        /* 
        using var batch = store.Backend.CreateBatch();
        using var writer = new PooledMemoryBufferWriter();

        var changes = false;
        foreach (var segment in store.CurrentSnapshot.DatomsChunked(SliceDescriptor.All, 1024))
        {
            foreach (var datom in segment)
            {
                var prefix = datom.Prefix;
                writer.Reset();
                var type = _function(ref prefix, datom.ValueSpan, writer);
                switch (type)
                {
                    case ScanResultType.None:
                        break;
                    case ScanResultType.Update:
                        batch.Delete(datom);
                        var newDatom = new Datom(prefix, writer.WrittenMemory); 
                        batch.Add(newDatom);
                        changes = true;
                        break;
                    case ScanResultType.Delete:
                        batch.Delete(datom);
                        changes = true;
                        break;
                }
            }
        }
        
        if (changes)
        {
            store.LogDatoms(batch, Array.Empty<Datom>(), advanceTx: false);
        }
        */
    }
}
