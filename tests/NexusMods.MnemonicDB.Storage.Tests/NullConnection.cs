using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Storage.Tests;

public class NullConnection : IConnection
{
    public IDb Db => throw new NotSupportedException();
    public IAttributeRegistry Registry => throw new NotSupportedException();
    public TxId TxId => throw new NotSupportedException();
    public IObservable<IDb> Revisions => throw new NotSupportedException();
    public IServiceProvider ServiceProvider => throw new NotSupportedException();
    public IDb AsOf(TxId txId)
    {
        throw new NotSupportedException();
    }

    public ITransaction BeginTransaction()
    {
        throw new NotSupportedException();
    }
}
