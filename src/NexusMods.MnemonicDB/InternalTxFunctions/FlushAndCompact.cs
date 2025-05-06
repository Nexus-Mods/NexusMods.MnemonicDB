using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB.InternalTxFunctions;

/// <summary>
/// Performs a flush and compact operation on the backend.
/// </summary>
internal class FlushAndCompact(bool verify) : AInternalFn
{
    public override void Execute(DatomStore store)
    {
        store.Backend.FlushAndCompact(verify);
    }
}
