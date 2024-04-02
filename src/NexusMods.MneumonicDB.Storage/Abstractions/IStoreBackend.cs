using System;
using NexusMods.MneumonicDB.Abstractions;
using NexusMods.MneumonicDB.Abstractions.DatomIterators;
using NexusMods.MneumonicDB.Storage.Abstractions.DatomComparators;
using NexusMods.MneumonicDB.Storage.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.MneumonicDB.Storage.Abstractions;

public interface IStoreBackend : IDisposable
{
    public IWriteBatch CreateBatch();

    public void Init(AbsolutePath location);

    public void DeclareIndex<TComparator>(IndexType name)
        where TComparator : IDatomComparator<AttributeRegistry>;


    public IIndex GetIndex(IndexType name);

    /// <summary>
    ///     Gets a snapshot of the current state of the store that will not change
    ///     during calls to GetIterator
    /// </summary>
    public ISnapshot GetSnapshot();

    /// <summary>
    ///     Create an EAVT index
    /// </summary>
    public void DeclareEAVT(IndexType name) => DeclareIndex<EAVTComparator<AttributeRegistry>>(name);

    /// <summary>
    ///     Create an AEVT index
    /// </summary>
    public void DeclareAEVT(IndexType name) => DeclareIndex<AEVTComparator<AttributeRegistry>>(name);

    /// <summary>
    ///     Create an AEVT index
    /// </summary>
    public void DeclareTxLog(IndexType name) => DeclareIndex<TxLogComparator<AttributeRegistry>>(name);

    /// <summary>
    ///     Create a backref index
    /// </summary>
    /// <param name="name"></param>
    public void DeclareVAET(IndexType name) => DeclareIndex<VAETComparator<AttributeRegistry>>(name);

    /// <summary>
    ///     Create a backref index
    /// </summary>
    /// <param name="name"></param>
    public void DeclareAVET(IndexType name) => DeclareIndex<AVETComparator<AttributeRegistry>>(name);
}
