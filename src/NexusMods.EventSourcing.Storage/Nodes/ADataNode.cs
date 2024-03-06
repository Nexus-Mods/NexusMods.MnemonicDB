using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Nodes;

public abstract partial class ADataNode : IDataNode
{
    #region Partial Methods

    public abstract IEnumerator<Datom> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public abstract int Length { get; }
    public abstract IColumn<EntityId> EntityIds { get; }
    public abstract IColumn<AttributeId> AttributeIds { get; }
    public abstract IColumn<TxId> TransactionIds { get; }
    public abstract IColumn<DatomFlags> Flags { get; }
    public abstract IBlobColumn Values { get; }

    public abstract Datom this[int idx] { get; }

    public abstract Datom LastDatom { get; }
    public abstract void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>;
    public abstract IDataNode Flush(INodeStore store);


    #endregion


    public virtual int FindEATV(int start, int end, in Datom target, IAttributeRegistry registry)
    {
        start = 0;
        end = EntityIds.Length;

        Debug.Assert(start <= end, "Start index should be less than or equal to end index");
        Debug.Assert(end <= EntityIds.Length, "End index should be less than or equal to the length of the node");
        while (start < end)
        {
            var mid = start + (end - start) / 2;

            var cmp = target.E.CompareTo(EntityIds[mid]);
            if (cmp == 0)
            {
                var attrId = AttributeIds[mid];
                var attrCmp = target.A.CompareTo(attrId);
                if (attrCmp == 0)
                {
                    var tmpCmp = target.T.CompareTo(TransactionIds[mid]);
                    if (tmpCmp == 0)
                    {
                        cmp = registry.CompareValues(attrId, target.V.Span, Values[mid].Span);
                    }
                    else
                    {
                        cmp = -tmpCmp;
                    }
                }
                else
                {
                    cmp = attrCmp;
                }
            }

            if (cmp > 0)
            {
                start = mid + 1;
            }
            else
            {
                end = mid;
            }
        }
        return start;
    }

    public virtual int FindAETV(int start, int end, in Datom target, IAttributeRegistry registry)
    {
        while (start < end)
        {
            var mid = start + (end - start) / 2;

            var cmp = target.A.CompareTo(AttributeIds[mid]);
            if (cmp == 0)
            {
                var entCmp = target.E.CompareTo(EntityIds[mid]);
                if (entCmp == 0)
                {
                    var tCmp = -target.T.CompareTo(TransactionIds[mid]);
                    if (tCmp == 0)
                    {
                        cmp = registry.CompareValues(target.A, target.V.Span, Values[mid].Span);
                    }
                    else
                    {
                        cmp = tCmp;
                    }

                }
                else
                {
                    cmp = entCmp;
                }
            }

            if (cmp > 0)
            {
                start = mid + 1;
            }
            else
            {
                end = mid;
            }
        }
        return start;
    }

    public virtual int FindAVTE(int start, int end, in Datom target, IAttributeRegistry registry)
    {
        while (start < end)
        {
            var mid = start + (end - start) / 2;

            var cmp = target.A.CompareTo(AttributeIds[mid]);
            if (cmp == 0)
            {
                var valueCmp = registry.CompareValues(target.A, target.V.Span, Values[mid].Span);
                if (valueCmp == 0)
                {
                    var tCmp = -target.T.CompareTo(TransactionIds[mid]);
                    if (tCmp == 0)
                    {
                        cmp = target.E.CompareTo(EntityIds[mid]);
                    }
                    else
                    {
                        cmp = tCmp;
                    }
                }
                else
                {
                    cmp = valueCmp;
                }
            }

            if (cmp > 0)
            {
                start = mid + 1;
            }
            else
            {
                end = mid;
            }
        }
        return start;
    }

    /// <summary>
    /// Helper dispatch function for the different sort order and find methods.
    /// </summary>
    public int Find(int start, int end, in Datom target, SortOrders order, IAttributeRegistry registry)
    {
        return order switch
        {
            SortOrders.EATV => FindEATV(start, end, target, registry),
            SortOrders.AETV => FindAETV(start, end, target, registry),
            SortOrders.AVTE => FindAVTE(start, end, target, registry),
            _ => throw new ArgumentOutOfRangeException(nameof(order), order, "Unknown sort order")
        };
    }
}
