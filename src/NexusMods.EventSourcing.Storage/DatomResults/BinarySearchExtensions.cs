using System;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.ChunkedEnumerables;

namespace NexusMods.EventSourcing.Storage.DatomResults;

public static class BinarySearchExtensions
{
    private static long FindEATVFromResult(this IDatomResult readable, in Datom target, IAttributeRegistry registry)
    {
        var start = 0L;
        var end = readable.Length;

        while (start < end)
        {
            var mid = start + (end - start) / 2;

            var cmp = target.E.CompareTo(readable.GetEntityId(mid));
            if (cmp == 0)
            {
                var attrId = readable.GetAttributeId(mid);
                var attrCmp = target.A.CompareTo(attrId);
                if (attrCmp == 0)
                {
                    var tmpCmp = target.T.CompareTo(readable.GetTransactionId(mid));
                    if (tmpCmp == 0)
                    {
                        cmp = registry.CompareValues(attrId, target.V.Span, readable.GetValue(mid));
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


    private static long FindAETVFromResult(this IDatomResult src, in Datom target, IAttributeRegistry registry)
    {
        var start = 0L;
        var end = src.Length;

        while (start < end)
        {
            var mid = start + (end - start) / 2;

            var cmp = target.A.CompareTo(src.GetAttributeId(mid));
            if (cmp == 0)
            {
                var entCmp = target.E.CompareTo(src.GetEntityId(mid));
                if (entCmp == 0)
                {
                    var tCmp = -target.T.CompareTo(src.GetTransactionId(mid));
                    if (tCmp == 0)
                    {
                        cmp = registry.CompareValues(target.A, target.V.Span, src.GetValue(mid));
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

    private static long FindAVTEFromResult(this IDatomResult src, in Datom target, IAttributeRegistry registry)
    {
        var start = 0L;
        var end = src.Length;

        while (start < end)
        {
            var mid = start + (end - start) / 2;

            var cmp = target.A.CompareTo(src.GetAttributeId(mid));
            if (cmp == 0)
            {
                var valueCmp = registry.CompareValues(target.A, target.V.Span, src.GetValue(mid));
                if (valueCmp == 0)
                {
                    var tCmp = -target.T.CompareTo(src.GetTransactionId(mid));
                    if (tCmp == 0)
                    {
                        cmp = target.E.CompareTo(src.GetEntityId(mid));
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
    /// Finds the index of the first occurrence of the given <see cref="Datom"/> in the <see cref="IReadable"/>.
    /// </summary>
    public static long Find(this IDatomResult readable, in Datom target, SortOrders order, IAttributeRegistry registry)
    {
        return order switch
        {
            SortOrders.EATV => readable.FindEATVFromResult(target, registry),
            SortOrders.AETV => readable.FindAETVFromResult(target, registry),
            SortOrders.AVTE => readable.FindAVTEFromResult(target, registry),
            _ => throw new ArgumentOutOfRangeException(nameof(order), order, "Unknown sort order")
        };
    }



}
