using System;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB.InternalTxFunctions;

internal abstract class AInternalFn : IInternalTxFunctionImpl
{
    private readonly TaskCompletionSource<(StoreResult, IDb)> _source = new();
    
    /// <summary>
    /// Execute the function on the store
    /// </summary>
    /// <param name="store"></param>
    public abstract void Execute(DatomStore store);

    public Task<(StoreResult, IDb)> Task => _source.Task;
    
    public void Complete(StoreResult result, IDb db)
    {
        _source.SetResult((result, db));
    }

    public void SetException(Exception exception)
    {
        _source.SetException(exception);
    }
}
