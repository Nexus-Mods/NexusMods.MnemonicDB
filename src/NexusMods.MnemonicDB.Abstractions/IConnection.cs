using System;
using System.Buffers;
using System.Threading.Tasks;
using DynamicData;
using NexusMods.HyperDuck;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions;


using DatomChangeSet = ChangeSet<Datom, DatomKey, IDb>;

/// <summary>
///     Represents a connection to a database.
/// </summary>
public interface IConnection : IDisposable, IQueryMixin
{
    /// <summary>
    ///     Gets the current database.
    /// </summary>
    public IDb Db { get; }
    
    /// <summary>
    /// The associated attribute resolver.
    /// </summary>
    AttributeResolver AttributeResolver { get; }
    
    /// <summary>
    /// The attribute cache for this connection.
    /// </summary>
    AttributeCache AttributeCache { get; }

    /// <summary>
    ///     Gets the most recent transaction id.
    /// </summary>
    public TxId TxId { get; }

    /// <summary>
    ///     A sequential stream of database revisions.
    /// </summary>
    public IObservable<IDb> Revisions { get; }

    /// <summary>
    /// A service provider that entities can use to resolve their values
    /// </summary>
    public IServiceProvider ServiceProvider { get; }
    
    /// <summary>
    /// Gets the datom store that this connection uses to store data.
    /// </summary>
    public IDatomStore DatomStore { get; }

    /// <summary>
    /// Returns a snapshot of the database as of the given transaction id.
    /// </summary>
    public IDb AsOf(TxId txId);

    /// <summary>
    /// Returns a snapshot of the database that contains all current and historical datoms.
    /// </summary>
    public IDb History();

    /// <summary>
    ///     Starts a new transaction.
    /// </summary>
    /// <returns></returns>
    public MainTransaction BeginTransaction();
    
    /// <summary>
    /// Deletes the entities with the given ids, also deleting them from any historical indexes. Returns the total number
    /// of datoms that were excised.
    /// </summary>
    public Task<ICommitResult> Excise(EntityId[] entityIds);
    
    /// <summary>
    /// Flushes the in-memory transaction log to the database, and compacts the database to remove any unused space.
    /// </summary>
    public Task<ICommitResult> FlushAndCompact(bool verify = false);

    /// <summary>
    /// Update the database's schema with the given attributes.
    /// </summary>
    public Task UpdateSchema(params IAttribute[] attribute);

    public Task<ICommitResult> Commit(Datoms datoms);

    /// <summary>
    /// Observe a slice of the database, as datoms are added or removed from the database, the observer will be updated
    /// with the changeset of datoms that have been added or removed.
    /// </summary>
    IObservable<DatomChangeSet> ObserveDatoms<TDescriptor>(TDescriptor descriptor) where TDescriptor : ISliceDescriptor;
    
    /// <summary>
    /// This delegate is called for each datom in the scan, the result time defines what should be done with the datom
    /// if None, no changes are made. If Update, the datom is updated via the value written to the valueOutput buffer.
    /// if Delete, the datom is deleted.
    /// </summary>
    public delegate ScanResultType ScanFunction(ref Datom datom);

    /// <summary>
    /// Update the data with the given scan function. This function will be handed every datom in the database (in
    /// an undefined order, and on an undefined number of threads). The function should return a ScanResultType that
    /// defines what should be done with each datom.
    /// </summary>
    public Task<ICommitResult> ScanUpdate(ScanFunction function);
}
