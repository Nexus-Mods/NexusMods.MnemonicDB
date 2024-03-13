using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Storage.Nodes.DataNode;

public abstract partial class ADataNode : IDataNode
{
    #region Partial Methods

    public abstract IEnumerator<Datom> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public abstract int Length { get; }

    public abstract long DeepLength { get; }
    public abstract Datom this[int idx] { get; }

    public abstract Datom LastDatom { get; }
    public abstract void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>;
    public abstract IDataNode Flush(INodeStore store);


    #endregion

    #region Lookup Methods

    /// <summary>
    /// Gets the entity id at the given index.
    /// </summary>
    public abstract EntityId GetEntityId(int idx);

    /// <summary>
    /// Gets the attribute id at the given index.
    /// </summary>
    public abstract AttributeId GetAttributeId(int idx);

    /// <summary>
    /// Gets the transaction id at the given index.
    /// </summary>
    public abstract TxId GetTransactionId(int idx);

    /// <summary>
    /// Gets the flags at the given index.
    /// </summary>
    public abstract ReadOnlySpan<byte> GetValue(int idx);


    #endregion


    public virtual int FindEATV(int start, int end, in Datom target, IAttributeRegistry registry)
    {
        start = 0;
        end = Length;

        Debug.Assert(start <= end, "Start index should be less than or equal to end index");
        Debug.Assert(end <= Length, "End index should be less than or equal to the length of the node");
        while (start < end)
        {
            var mid = start + (end - start) / 2;

            var cmp = target.E.CompareTo(GetEntityId(mid));
            if (cmp == 0)
            {
                var attrId = GetAttributeId(mid);
                var attrCmp = target.A.CompareTo(attrId);
                if (attrCmp == 0)
                {
                    var tmpCmp = target.T.CompareTo(GetTransactionId(mid));
                    if (tmpCmp == 0)
                    {
                        cmp = registry.CompareValues(attrId, target.V.Span, GetValue(mid));
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

            var cmp = target.A.CompareTo(GetAttributeId(mid));
            if (cmp == 0)
            {
                var entCmp = target.E.CompareTo(GetEntityId(mid));
                if (entCmp == 0)
                {
                    var tCmp = -target.T.CompareTo(GetTransactionId(mid));
                    if (tCmp == 0)
                    {
                        cmp = registry.CompareValues(target.A, target.V.Span, GetValue(mid));
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

            var cmp = target.A.CompareTo(GetAttributeId(mid));
            if (cmp == 0)
            {
                var valueCmp = registry.CompareValues(target.A, target.V.Span, GetValue(mid));
                if (valueCmp == 0)
                {
                    var tCmp = -target.T.CompareTo(GetTransactionId(mid));
                    if (tCmp == 0)
                    {
                        cmp = target.E.CompareTo(GetEntityId(mid));
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
