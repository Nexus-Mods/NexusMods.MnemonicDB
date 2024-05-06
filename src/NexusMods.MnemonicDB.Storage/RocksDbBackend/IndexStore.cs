using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomComparators;
using NexusMods.MnemonicDB.Storage.Abstractions;
using RocksDbSharp;

namespace NexusMods.MnemonicDB.Storage.RocksDbBackend;

public class IndexStore<TComparator>(string handleName, IndexType type) : IRocksDBIndexStore
    where TComparator : IDatomComparator
{
    private IntPtr _comparator;
    private CompareDelegate _comparatorDelegate = null!;
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
    private static readonly List<object> Roots = new ();

    public ColumnFamilyHandle Handle { get; private set; } = null!;

    public IndexType Type { get; } = type;

    public void SetupColumnFamily(IIndex index, ColumnFamilies columnFamilies)
    {
        _options = new ColumnFamilyOptions();
        _namePtr = Marshal.StringToHGlobalAnsi(handleName);

        _nameDelegate = _ => _namePtr;
        _destructorDelegate = static _ => { };
        _comparatorDelegate = static (_, a, alen, b, blen) =>
        {
            unsafe
            {
                return TComparator.Compare((byte*)a, (int)alen, (byte*)b, (int)blen);
            }
        };

        // Save these as roots so they never get GC'd
        Roots.Add((_nameDelegate, _destructorDelegate, _comparatorDelegate));

        _comparator =
            Native.Instance.rocksdb_comparator_create(IntPtr.Zero, _destructorDelegate, _comparatorDelegate,
                _nameDelegate);
        _options.SetComparator(_comparator);

        columnFamilies.Add(handleName, _options);
    }

    public void PostOpenSetup(RocksDb db)
    {
        Handle = db.GetColumnFamily(handleName);
    }
}
