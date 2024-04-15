using System;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Storage.Abstractions;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

public class IndexStore : IIndexStore
{
    private readonly string _handleName;
    private readonly AttributeRegistry _registry;
    private IntPtr _comparator;
    private CompareDelegate _comparatorDelegate = null!;
    private RocksDb _db = null!;
    private DestructorDelegate _destructorDelegate = null!;
    private NameDelegate _nameDelegate = null!;
    private IntPtr _namePtr;
    private ColumnFamilyOptions _options = null!;

    public IndexStore(string handleName, IndexType type, AttributeRegistry registry)
    {
        Type = type;
        _registry = registry;
        _handleName = handleName;
    }

    public ColumnFamilyHandle Handle { get; private set; } = null!;

    public IndexType Type { get; }


    public void SetupColumnFamily(IIndex index, ColumnFamilies columnFamilies)
    {
        _options = new ColumnFamilyOptions();
        _namePtr = Marshal.StringToHGlobalAnsi(_handleName);

        _nameDelegate = _ => _namePtr;
        _destructorDelegate = _ => { };
        _comparatorDelegate = (_, a, alen, b, blen) =>
        {
            unsafe
            {
                AValueSerializer.CompareValues((byte*)a, alen, (byte*)b, blen);
            }
        };

        _comparator =
            Native.Instance.rocksdb_comparator_create(IntPtr.Zero, _destructorDelegate, _comparatorDelegate,
                _nameDelegate);
        _options.SetComparator(_comparator);

        columnFamilies.Add(_handleName, _options);
    }

    public void PostOpenSetup(RocksDb db)
    {
        _db = db;
        Handle = db.GetColumnFamily(_handleName);
    }
}
