using NexusMods.MnemonicDB.Storage.Abstractions;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

public interface IRocksDBIndexStore : IIndexStore
{

    /// <summary>
    /// Setup the column family for the index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="columnFamilies"></param>
    public void SetupColumnFamily(IIndex index, ColumnFamilies columnFamilies);

    /// <summary>
    /// Called after the database is opened and all indexes are configured
    /// </summary>
    /// <param name="db"></param>
    public void PostOpenSetup(RocksDb db);


    /// <summary>
    /// Gets the RocksDB column family handle
    /// </summary>
    ColumnFamilyHandle Handle { get; }

}
