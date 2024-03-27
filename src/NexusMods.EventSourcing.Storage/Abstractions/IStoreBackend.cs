
using System;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.Storage.Abstractions;

public interface IStoreBackend : IDisposable
{
    public IWriteBatch CreateBatch();

    public void Init(AbsolutePath location);

    public void DeclareIndex<TA, TB, TC, TD, TF>(IndexType name)
        where TA : IElementComparer
        where TB : IElementComparer
        where TC : IElementComparer
        where TD : IElementComparer
        where TF : IElementComparer;


    public IIndex GetIndex(IndexType name);

    /// <summary>
    /// Gets a snapshot of the current state of the store that will not change
    /// during calls to GetIterator
    /// </summary>
    public ISnapshot GetSnapshot();

    /// <summary>
    /// Create an EAVT index
    /// </summary>
    public void DeclareEAVT(IndexType name) =>
        DeclareIndex<EComparer, AComparer, ValueComparer, TxComparer, AssertComparer>
            (name);

    /// <summary>
    /// Create an AEVT index
    /// </summary>
    public void DeclareAEVT(IndexType name) =>
        DeclareIndex<AComparer, EComparer, ValueComparer, TxComparer, AssertComparer>
            (name);

    /// <summary>
    /// Create an AEVT index
    /// </summary>
    public void DeclareTxLog(IndexType name) =>
        DeclareIndex<TxComparer, EComparer, AComparer, ValueComparer, AssertComparer>
            (name);

    /// <summary>
    /// Create a backref index
    /// </summary>
    /// <param name="name"></param>
    public void DeclareVAET(IndexType name) =>
        DeclareIndex<UnmanagedValueComparer<ulong>, AComparer, EComparer, TxComparer, AssertComparer>(name);

    /// <summary>
    /// Create a backref index
    /// </summary>
    /// <param name="name"></param>
    public void DeclareAVET(IndexType name) =>
        DeclareIndex<AComparer, ValueComparer, EComparer, TxComparer, AssertComparer>(name);
}
