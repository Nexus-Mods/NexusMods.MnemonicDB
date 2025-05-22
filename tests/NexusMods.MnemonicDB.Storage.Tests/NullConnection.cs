using DynamicData;
using NexusMods.Cascade;
using NexusMods.Cascade.Abstractions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Internals;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.MnemonicDB.Storage.Tests;

public class NullConnection : IConnection
{
    public Topology Topology => throw new NotSupportedException();
    public IDb Db => throw new NotSupportedException();
    public AttributeResolver AttributeResolver => throw new NotSupportedException();
    public AttributeCache AttributeCache => throw new NotSupportedException();
    public TxId TxId => throw new NotSupportedException();
    public IObservable<IDb> Revisions => throw new NotSupportedException();
    public IServiceProvider ServiceProvider => throw new NotSupportedException();
    public IDatomStore DatomStore => throw new NotSupportedException();

    public IDb AsOf(TxId txId)
    {
        throw new NotSupportedException();
    }

    public IDb History()
    {
        throw new NotSupportedException();
    }

    public IMainTransaction BeginTransaction()
    {
        throw new NotSupportedException();
    }

    public IAnalyzer[] Analyzers => throw new NotSupportedException();
    public Task<ICommitResult> Excise(EntityId[] entityIds)
    {
        throw new NotSupportedException();
    }


    public Task<ICommitResult> FlushAndCompact(bool verify)
    {
        throw new NotSupportedException();
    }

    public Task UpdateSchema(params IAttribute[] attribute)
    {
        throw new NotSupportedException();
    }

    public IObservable<ChangeSet<Datom, DatomKey, IDb>> ObserveDatoms<TDescriptor>(TDescriptor descriptor) where TDescriptor : ISliceDescriptor
    {
        throw new NotSupportedException();
    }

    public IObservable<ChangeSet<Datom, DatomKey, IDb>> ObserveDatoms(SliceDescriptor descriptor)
    {
        throw new NotSupportedException();
    }

    public Task<ICommitResult> ScanUpdate(IConnection.ScanFunction function)
    {
        throw new NotSupportedException();
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}
