using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DynamicData;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Storage.Abstractions;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

public class IndexStore<TComparator> : IRocksDBIndexStore
where TComparator : IDatomComparator
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

    /// <summary>
    /// This is a bit of a hack, but we throw all our interop delegates in here, and then they
    /// live for the entire life of the application. It seems that RocksDB will occasionally call
    /// delegates after we think we've disposed of the handles. It really doesn't matter as these
    /// things will never amount to more than a few dozen objects.
    /// </summary>
    /// <returns></returns>
    private static List<object> _roots = new ();

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
                return TComparator.Compare((byte*)a, (int)alen, (byte*)b, (int)blen);
            }
        };

        // Save these as roots so they never get GC'd
        _roots.Add((_nameDelegate, _destructorDelegate, _comparatorDelegate));

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
