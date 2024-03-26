using System;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.Abstractions;
using Reloaded.Memory.Extensions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.Storage.RocksDbBackend;

public class IndexStore : IIndexStore
{
    private readonly string _handleName;
    private ColumnFamilyOptions _options = null!;
    private IntPtr _namePtr;
    private NameDelegate _nameDelegate = null!;
    private DestructorDelegate _destructorDelegate = null!;
    private CompareDelegate _comparatorDelegate = null!;
    private IntPtr _comparator;
    private ColumnFamilyHandle _columnHandle = null!;
    private RocksDb _db = null!;
    private readonly AttributeRegistry _registry;

    public IndexStore(string handleName, IndexType type, AttributeRegistry registry)
    {
        Type = type;
        _registry = registry;
        _handleName = handleName;
    }

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
                return index.Compare(new ReadOnlySpan<byte>((void*)a, (int)alen), new ReadOnlySpan<byte>((void*)b, (int)blen));
            }
        };
        _comparator = Native.Instance.rocksdb_comparator_create(IntPtr.Zero, _destructorDelegate, _comparatorDelegate, _nameDelegate);
        _options.SetComparator(_comparator);

        columnFamilies.Add(_handleName, _options);
    }

    public ColumnFamilyHandle Handle => _columnHandle;

    public void PostOpenSetup(RocksDb db)
    {
        _db = db;
        _columnHandle = db.GetColumnFamily(_handleName);
    }


    public IDatomIterator GetIterator()
    {
        return new IteratorWrapper(_db.NewIterator(_columnHandle));

    }
}
