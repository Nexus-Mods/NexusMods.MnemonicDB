using System;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Storage.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Storage.Abstractions;

public interface IStoreBackend : IDisposable
{
    public IWriteBatch CreateBatch();

    public void Init(AbsolutePath location);

    public void DeclareIndex<TComparator>(IndexType name)
        where TComparator : IDatomComparator;


    public IIndex GetIndex(IndexType name);

    /// <summary>
    ///     Gets a snapshot of the current state of the store that will not change
    ///     during calls to GetIterator
    /// </summary>
    public ISnapshot GetSnapshot();

    /// <summary>
    ///     Create an EAVT index
    /// </summary>
    public void DeclareEAVT(IndexType name) => DeclareIndex<EAVTComparator>(name);

    /// <summary>
    ///     Create an AEVT index
    /// </summary>
    public void DeclareAEVT(IndexType name) => DeclareIndex<AEVTComparator>(name);

    /// <summary>
    ///     Create an AEVT index
    /// </summary>
    public void DeclareTxLog(IndexType name) => DeclareIndex<TxLogComparator>(name);

    /// <summary>
    ///     Create a backref index
    /// </summary>
    /// <param name="name"></param>
    public void DeclareVAET(IndexType name) => DeclareIndex<VAETComparator>(name);

    /// <summary>
    ///     Create a backref index
    /// </summary>
    /// <param name="name"></param>
    public void DeclareAVET(IndexType name) => DeclareIndex<AVETComparator>(name);
}
