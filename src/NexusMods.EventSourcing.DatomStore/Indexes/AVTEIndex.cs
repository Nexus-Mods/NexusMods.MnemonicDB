using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using Reloaded.Memory.Extensions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.DatomStore.Indexes;

public class AVTEIndex(AttributeRegistry registry) :
    AIndexDefinition<AVTEIndex>(registry, "avte"), IComparatorIndex<AVTEIndex>
{
    public static unsafe int Compare(AIndexDefinition<AVTEIndex> idx, KeyHeader* a, uint aLength, KeyHeader* b, uint bLength)
    {
        // Attribute, Value, TX, Entity
        var cmp = KeyHeader.CompareAttribute(a, b);
        if (cmp != 0) return cmp;
        cmp = KeyHeader.CompareValues(idx.Registry, a, aLength, b, bLength);
        if (cmp != 0) return cmp;
        cmp = KeyHeader.CompareTx(a, b);
        if (cmp != 0) return cmp;
        cmp = KeyHeader.CompareEntity(a, b);
        if (cmp != 0) return cmp;
        return KeyHeader.CompareIsAssert(a, b);
    }

    public unsafe struct AVTEIterator : IDisposable
    {
        private readonly KeyHeader* _key;
        private KeyHeader* _current;
        private UIntPtr _currentLength;
        private readonly Iterator _iterator;
        private readonly AttributeRegistry _registry;
        private bool _needsSeek;

        public AVTEIterator(ulong txId, AttributeRegistry registry, AVTEIndex idx)
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


        public void Set<TAttribute>() where TAttribute : IAttribute
        {
            _key->Entity = ulong.MaxValue;
            _key->AttributeId = _registry.GetAttributeId<TAttribute>();
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

        public EntityId EntityId => EntityId.From(_current->Entity);

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
                _key->Entity = _current->Entity - 1;
                _iterator.Prev();
            }

            while (true)
            {

                if (!_iterator.Valid()) return false;

                _current = (KeyHeader*)Native.Instance.rocksdb_iter_key(_iterator.Handle, out _currentLength);

                Debug.Assert(_currentLength < KeyHeader.Size, "Key length is less than KeyHeader.Size");

                if (_current->AttributeId != _key->AttributeId)
                    return false;

                if (_current->Tx > _key->Tx)
                {
                    _iterator.Prev();
                    continue;
                }

                if (_current->Entity > _key->Entity)
                {
                    _iterator.Prev();
                    continue;
                }

                return true;
            }
        }
        public void Dispose()
        {
            _iterator.Dispose();
            Marshal.FreeHGlobal((IntPtr)_key);
        }
    }
}
