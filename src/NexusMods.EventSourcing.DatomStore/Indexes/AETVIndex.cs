using System;
using System.Runtime.InteropServices;
using NexusMods.EventSourcing.Abstractions;
using Reloaded.Memory.Extensions;
using RocksDbSharp;

namespace NexusMods.EventSourcing.DatomStore.Indexes;

public class AETVIndex(AttributeRegistry registry) : AIndexDefinition<AETVIndex>(registry, "aetv"), IComparatorIndex<AETVIndex>
{
    public static unsafe int Compare(AIndexDefinition<AETVIndex> idx, KeyHeader* a, uint aLength, KeyHeader* b, uint bLength)
    {
        // TX, Entity, Attribute, IsAssert, Value
        var cmp = KeyHeader.CompareAttribute(a, b);
        if (cmp != 0) return cmp;
        cmp = KeyHeader.CompareEntity(a, b);
        if (cmp != 0) return cmp;
        cmp = KeyHeader.CompareTx(a, b);
        if (cmp != 0) return cmp;
        cmp = KeyHeader.CompareIsAssert(a, b);
        if (cmp != 0) return cmp;
        return KeyHeader.CompareValues(idx.Registry, a, aLength, b, bLength);
    }


    public struct AEVTByAIterator<TAttr> : IIterator
        where TAttr : IAttribute
    {
        private KeyHeader _key;
        private readonly Iterator _iterator;
        private readonly ulong _attrId;
        private readonly AttributeRegistry _registry;

        public AEVTByAIterator(ulong txId, AttributeRegistry registry, AETVIndex idx)
        {
            _registry = registry;
            _iterator = idx.Db.NewIterator(idx.ColumnFamilyHandle);
            _attrId = registry.GetAttributeId<TAttr>();
            _key = new KeyHeader
            {
                Entity = UInt64.MaxValue,
                AttributeId = _attrId,
                Tx = txId,
                IsAssert = true,
            };
            _iterator.Seek(MemoryMarshal.CreateSpan(ref _key, KeyHeader.Size).CastFast<KeyHeader, byte>());
        }

        private ReadOnlySpan<KeyHeader> GetCurrentSpan() => MemoryMarshal.Cast<byte, KeyHeader>(_iterator.GetKeySpan());

        public EntityId EntityId => EntityId.From(GetCurrentSpan()[0].Entity);

        public bool IsAttribute<TAttribute>() where TAttribute : IAttribute
        {
            return GetCurrentSpan()[0].AttributeId == _attrId;
        }

        public TxId TxId => TxId.From(GetCurrentSpan()[0].Tx);

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

        public bool Next()
        {
        TOP:
            _iterator.Prev();

            if (!_iterator.Valid()) return false;

            var current = _iterator.GetKeySpan();
            var currentHeader = MemoryMarshal.AsRef<KeyHeader>(current);

            if (currentHeader.AttributeId != _attrId) return false;

            if (currentHeader.IsRetraction)
            {
                _key.Entity = currentHeader.Entity - 1;
                goto TOP;
            }
            return true;
        }

        public void Reset()
        {
            _key.Entity = UInt64.MaxValue;
            _iterator.SeekForPrev(MemoryMarshal.CreateSpan(ref _key, KeyHeader.Size).CastFast<KeyHeader, byte>());
        }

        public void Dispose()
        {
            _iterator.Dispose();
        }
    }
}
