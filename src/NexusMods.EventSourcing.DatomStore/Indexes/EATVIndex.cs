using System;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;
using Reloaded.Memory.Extensions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.DatomStore.Indexes;

public class EATVIndex(AttributeRegistry registry) : AIndexDefinition(registry, "eatv")
{
    public override unsafe int Compare(KeyHeader* a, uint aLength, KeyHeader* b, uint bLength)
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
        return KeyHeader.CompareValues(Registry, a, aLength, b, bLength);
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
        private readonly EATVIndex _idx;
        private KeyHeader* _key;
        private readonly Iterator _iterator;
        private readonly ulong _attrId;
        private readonly AttributeRegistry _registry;
        private bool _justSet;

        public EATVIterator(ulong txId, AttributeRegistry registry, EATVIndex idx)
        {
            _idx = idx;
            _registry = registry;
            _iterator = idx.Db.NewIterator(idx.ColumnFamilyHandle);
            _key = (KeyHeader*)Marshal.AllocHGlobal(KeyHeader.Size);
            _key->Entity = ulong.MaxValue;
            _key->AttributeId = ulong.MaxValue;
            _key->Tx = txId;
            _key->IsAssert = true;
            _iterator.SeekForPrev((byte*)_key, KeyHeader.Size);
            _justSet = true;
        }


        public void SeekTo(EntityId entityId)
        {
            _key->Entity = entityId.Value;
            _key->AttributeId = ulong.MaxValue;
            _iterator.SeekForPrev((byte*)_key, KeyHeader.Size);
            _justSet = true;
        }

        public IDatom Current
        {
            get
            {
                var span = _iterator.GetKeySpan();
                var valueSpan = span.SliceFast(KeyHeader.Size);
                var header = MemoryMarshal.AsRef<KeyHeader>(span);
                return _registry.ReadDatom(ref header, valueSpan);
            }
        }

        public TValue GetValue<TAttribute, TValue>()
            where TAttribute : IAttribute<TValue>
        {
            var span = _iterator.GetKeySpan();
            var currentHeader = MemoryMarshal.AsRef<KeyHeader>(span);
            var currentValue = span.SliceFast(KeyHeader.Size);
            return _registry.ReadValue<TAttribute, TValue>(ref currentHeader, currentValue);

        }

        public ulong AttributeId
        {
            get
            {
                var span = _iterator.GetKeySpan();
                var currentHeader = MemoryMarshal.AsRef<KeyHeader>(span);
                return currentHeader.AttributeId;
            }
        }

        public ReadOnlySpan<byte> ValueSpan => _iterator.GetKeySpan().SliceFast(KeyHeader.Size);

        public bool Next()
        {
        TOP:
            if (!_justSet)
            {
                _iterator.Prev();
            }
            else
            {
                _justSet = false;
            }

            if (!_iterator.Valid()) return false;

            var current = _iterator.GetKeySpan();
            var currentHeader = MemoryMarshal.AsRef<KeyHeader>(current);

            if (currentHeader.Entity != _key->Entity) return false;

            if (currentHeader.IsRetraction)
            {
                _key->AttributeId = currentHeader.AttributeId - 1;
                goto TOP;
            }
            return true;
        }
        public void Dispose()
        {
            _iterator.Dispose();
            Marshal.FreeHGlobal((IntPtr)_key);
        }
    }

}
