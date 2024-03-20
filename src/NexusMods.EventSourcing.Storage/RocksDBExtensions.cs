using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RocksDbSharp;

namespace NexusMods.EventSourcing.Storage;

public static class RocksDBExtensions
{
    private static readonly ReadOptions DefaultReadOptions = new();

    public ref struct ValueRef
    {
        private IntPtr _ptr;
        private int _length;

        public ValueRef(IntPtr ptr, int length)
        {
            _ptr = ptr;
            _length = length;
        }

        /// <summary>
        /// True if the value is valid
        /// </summary>
        public bool IsValid => _ptr != IntPtr.Zero;

        public ReadOnlySpan<byte> Span
        {
            get
            {
                unsafe
                {
                    return new ReadOnlySpan<byte>(_ptr.ToPointer(), _length);
                }
            }
        }

        public void Dispose()
        {
            if (_ptr != IntPtr.Zero)
                Native.Instance.rocksdb_free(_ptr);
        }
    }

    public static ValueRef GetScoped<TKey>(this RocksDb db, ref TKey key, ColumnFamilyHandle columnFamily)
    where TKey : unmanaged
    {
        unsafe
        {
            var keySpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref key, 1));
            fixed (byte *kPtr = keySpan)
            {
                var ptr = Native.Instance.rocksdb_get_cf(db.Handle, DefaultReadOptions.Handle, columnFamily.Handle, kPtr,
                    (UIntPtr)keySpan.Length, out var valLen, out var errPtr);
                if (ptr == IntPtr.Zero)
                {
                    return new ValueRef(IntPtr.Zero, 0);
                }
                if (errPtr != IntPtr.Zero)
                {
                    return new ValueRef(IntPtr.Zero, 0);
                }
                return new ValueRef(ptr, (int)valLen);
            }
        }
    }

    public struct IteratorValue
    {
        public Iterator _iterator;

        public ReadOnlySpan<byte> KeySpan => _iterator.Key();

        public ReadOnlySpan<byte> ValueSpan => _iterator.Value();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TKey Key<TKey>() where TKey : unmanaged
        {
            return MemoryMarshal.Cast<byte, TKey>(KeySpan)[0];
        }
    }

    public static IEnumerable<IteratorValue> GetScopedIterator<TKey>(this RocksDb db, TKey key, ColumnFamilyHandle columnFamily)
    where TKey : unmanaged
    {
        using var iterator = db.NewIterator(columnFamily);
        var keySpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref key, 1));
        iterator.Seek(keySpan);

        while (iterator.Valid())
        {
            yield return new IteratorValue { _iterator = iterator };
            iterator.Next();
        }
    }
}
