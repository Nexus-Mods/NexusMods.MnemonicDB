using System;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB;

/// <summary>
/// A TxFunction that is internal to the MnemonicDB, used mostly for schema updates
/// and other operations that are started via the TX Queue but are exposed to
/// users through non transaction APIs
/// </summary>
public interface IInternalTxFunctionImpl : IInternalTxFunction
{
    /// <summary>
    ///  Executes the function giving the function full access to the store
    /// </summary>
    public void Execute(DatomStore store, AttributeResolver resolver);

    /// <summary>
    /// A task that will complete when the transaction is committed
    /// </summary>
    public Task<(StoreResult, IDb)> Task { get; }
    
    /// <summary>
    /// Set the result of the transaction
    /// </summary>
    /// <param name="result"></param>
    /// <param name="db"></param>
    public void Complete(StoreResult result, IDb db);

    /// <summary>
    /// Sets the state of the transaction to an exception
    /// </summary>
    /// <param name="exception"></param>
    public void SetException(Exception exception);
}
