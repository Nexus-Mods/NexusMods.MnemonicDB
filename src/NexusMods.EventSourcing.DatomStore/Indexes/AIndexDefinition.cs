using System;
using System.Runtime.InteropServices;
using RocksDbSharp;

namespace NexusMods.EventSourcing.DatomStore.Indexes;

public abstract class AIndexDefinition(AttributeRegistry registry, string columnFamilyName) : IDisposable
{
    protected readonly AttributeRegistry Registry = registry;
    private ColumnFamilyOptions? _options;
    protected ColumnFamilyHandle? ColumnFamilyHandle;

    private DestructorDelegate? _destructorDelegate;
    private CompareDelegate? _comparatorDelegate;
    private NameDelegate? _nameDelegate;
    private IntPtr _namePtr;
    private IntPtr _comparator;
    protected RocksDb Db = null!;

    public void Init(RocksDb db)
    {
        Db = db;
        _options = new ColumnFamilyOptions();
        _namePtr = Marshal.StringToHGlobalAnsi(ColumnFamilyName);

        _nameDelegate = _ => _namePtr;
        _destructorDelegate = _ => { };
        _comparatorDelegate = (_, a, alen, b, blen) =>
        {
            unsafe
            {
                return Compare((KeyHeader*)a, (uint)alen, (KeyHeader*)b, (uint)blen);
            }
        };
        _comparator = Native.Instance.rocksdb_comparator_create(IntPtr.Zero, _destructorDelegate, _comparatorDelegate, _nameDelegate);
        _options.SetComparator(_comparator);
        ColumnFamilyHandle = db.CreateColumnFamily(_options, ColumnFamilyName);
    }

    public string ColumnFamilyName { get; } = columnFamilyName;

    public abstract unsafe int Compare(KeyHeader *a, uint aLength, KeyHeader *b, uint bLength);

    public void Dispose()
    {
        if (_comparator != IntPtr.Zero)
        {
            Native.Instance.rocksdb_comparator_destroy(_comparator);
            _comparator = IntPtr.Zero;
        }

        if (_namePtr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_namePtr);
            _namePtr = IntPtr.Zero;
        }
    }
    public void Put(WriteBatch batch, ReadOnlySpan<byte> span)
    {
        batch.Put(span, ReadOnlySpan<byte>.Empty, ColumnFamilyHandle);
    }

    public Iterator NewIterator()
    {
        var options = new ReadOptions();
        options.SetTotalOrderSeek(true);
        return Db.NewIterator(ColumnFamilyHandle, options);
    }
}
