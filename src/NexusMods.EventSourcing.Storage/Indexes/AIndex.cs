using System;
using System.Runtime.InteropServices;

using RocksDbSharp;

namespace NexusMods.EventSourcing.Storage.Indexes;

public abstract class AIndex
{
    protected RocksDb Db = null!;
    protected ColumnFamilyHandle ColumnFamily = null!;
    protected readonly AttributeRegistry Registry;
    private ColumnFamilyOptions _options = null!;
    private IntPtr _namePtr;
    private NameDelegate _nameDelegate = null!;
    private DestructorDelegate _destructorDelegate = null!;
    private CompareDelegate _comparatorDelegate = null!;
    private IntPtr _comparator;
    private readonly string _columnFamilyName;

    protected AIndex(string columnFamilyName, AttributeRegistry registry, ColumnFamilies columnFamilies)
    {
        Registry = registry;
        _columnFamilyName = columnFamilyName;

        _options = new ColumnFamilyOptions();
        _namePtr = Marshal.StringToHGlobalAnsi(_columnFamilyName);

        _nameDelegate = _ => _namePtr;
        _destructorDelegate = _ => { };
        _comparatorDelegate = (_, a, alen, b, blen) =>
        {
            unsafe
            {
                return Compare(new ReadOnlySpan<byte>((void*)a, (int)alen), new ReadOnlySpan<byte>((void*)b, (int)blen));
            }
        };
        _comparator = Native.Instance.rocksdb_comparator_create(IntPtr.Zero, _destructorDelegate, _comparatorDelegate, _nameDelegate);
        _options.SetComparator(_comparator);

        columnFamilies.Add(_columnFamilyName, _options);
    }

    public void Init(RocksDb db)
    {
        Db = db;
        ColumnFamily = Db.GetColumnFamily(_columnFamilyName);
    }

    protected abstract int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);


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

}
