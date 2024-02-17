using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using Reloaded.Memory.Extensions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.DatomStore.Indexes;

public class EATVIndex(AttributeRegistry registry) : AIndexDefinition<EATVIndex>(registry, "eatv"), IComparatorIndex<EATVIndex>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int Compare(AIndexDefinition<EATVIndex> idx, KeyHeader* a, uint aLength, KeyHeader* b, uint bLength)
    {
        // TX, Entity, Attribute, IsAssert, Value
        var cmp = KeyHeader.CompareEntity(a, b);
        if (cmp != 0) return cmp;
        cmp = KeyHeader.CompareAttribute(a, b);
        if (cmp != 0) return cmp;
        cmp = KeyHeader.CompareTx(a, b);
        if (cmp != 0) return cmp;
        cmp = KeyHeader.CompareIsAssert(a, b);
        if (cmp != 0) return cmp;
        return KeyHeader.CompareValues(idx.Registry, a, aLength, b, bLength);
    }


    public bool MaxId(Ids.Partition partition, out ulong o)
    {
        var key = new KeyHeader
        {
            Entity = Ids.MaxId(partition),
            AttributeId = ulong.MaxValue,
            Tx = ulong.MaxValue,
            IsAssert = true,
        };
        var span = MemoryMarshal.CreateSpan(ref key, KeyHeader.Size);
        using var it = Db.NewIterator(ColumnFamilyHandle);
        it.SeekForPrev(span.CastFast<KeyHeader, byte>());
        if (!it.Valid())
        {
            o = 0;
            return false;
        }
        var current = it.GetKeySpan();
        var currentHeader = MemoryMarshal.AsRef<KeyHeader>(current);
        var eVal = currentHeader.Entity;
        if (Ids.IsPartition(eVal, partition))
        {
            o = eVal;
            return true;
        }
        o = 0;
        return false;
    }

    public unsafe struct EATVIterator : IEntityIterator, IDisposable
    {
        private readonly KeyHeader* _key;
        private KeyHeader* _current;
        private UIntPtr _currentLength;
        private readonly Iterator _iterator;
        private readonly AttributeRegistry _registry;
        private bool _needsSeek;

        public EATVIterator(ulong txId, AttributeRegistry registry, EATVIndex idx)
        {
            _registry = registry;
            _iterator = idx.Db.NewIterator(idx.ColumnFamilyHandle);
            _key = (KeyHeader*)Marshal.AllocHGlobal(KeyHeader.Size);
            _key->Entity = ulong.MaxValue;
            _key->AttributeId = ulong.MaxValue;
            _key->Tx = txId;
            _key->IsAssert = true;
            _needsSeek = true;
        }


        public void Set(EntityId entityId)
        {
            _key->Entity = entityId.Value;
            _key->AttributeId = ulong.MaxValue;
            _needsSeek = true;
        }

        public IDatom Current
        {
            get
            {
                Debug.Assert(!_needsSeek, "Must call Next() before accessing Current");
                var currentValue = new ReadOnlySpan<byte>((byte*)_current + KeyHeader.Size, (int)_currentLength - KeyHeader.Size);
                return _registry.ReadDatom(ref *_current, currentValue);
            }
        }

        public TValue GetValue<TAttribute, TValue>()
            where TAttribute : IAttribute<TValue>
        {
            Debug.Assert(!_needsSeek, "Must call Next() before accessing GetValue");
            var currentValue = new ReadOnlySpan<byte>((byte*)_current + KeyHeader.Size, (int)_currentLength - KeyHeader.Size);
            return _registry.ReadValue<TAttribute, TValue>(ref *_current, currentValue);

        }

        public ulong AttributeId
        {
            get
            {
                Debug.Assert(!_needsSeek, "Must call Next() before accessing AttributeId");
                return _current->AttributeId;
            }
        }

        public ReadOnlySpan<byte> ValueSpan => _iterator.GetKeySpan().SliceFast(KeyHeader.Size);

        public bool Next()
        {
            if (_needsSeek)
            {
                _iterator.SeekForPrev((byte*)_key, KeyHeader.Size);
                _needsSeek = false;
            }
            else
            {
                if (_current->AttributeId == 0)
                    return false;

                _key->AttributeId = _current->AttributeId - 1;
                _iterator.SeekForPrev((byte*)_key, KeyHeader.Size);
            }

            if (!_iterator.Valid()) return false;

            _current = (KeyHeader*)Native.Instance.rocksdb_iter_key(_iterator.Handle, out _currentLength);

            Debug.Assert(_currentLength >= KeyHeader.Size, "Key length is less than KeyHeader.Size");

            if (_current->Entity != _key->Entity)
                return false;

            return true;
        }
        public void Dispose()
        {
            _iterator.Dispose();
            Marshal.FreeHGlobal((IntPtr)_key);
        }
    }

}
