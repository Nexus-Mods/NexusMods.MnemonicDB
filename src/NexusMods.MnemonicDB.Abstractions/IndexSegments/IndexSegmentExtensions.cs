using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// Extensions for working with index segments
/// </summary>
public static class IndexSegmentExtensions
{
    /// <summary>
    /// Merges two index segments by entity id, returning a list of the entity ids found in both segments. This is a
    /// union operation, and assumes that the two segments ar ordered by entity id.
    /// </summary>
    public static List<EntityId> MergeByEntityId(this IndexSegment setA, IndexSegment setB)
    {
        // Entity ids are stored in the lower ulong of the key prefix
        var lowerA = setA.Lowers;
        var lowerB = setB.Lowers;
        
        // This is a merge join, we find the intersection of the two sets based on the E value
        List<EntityId> result = new();
        
        var i = 0;
        var j = 0;
        while (true)
        {
            if (i >= lowerA.Length || j >= lowerB.Length)
                break;
            // Lop off the lower 8 bits as those are the flags
            var initialA = lowerA[i];
            var a = initialA >> 8;
            var b = lowerB[j] >> 8;
            
            if (a == b)
            {
                result.Add(EntityId.From((initialA & 0xFF00000000000000) | ((initialA >> 8) & 0x0000FFFFFFFFFFFF)));
                i++;
                j++;
            }
            else if (a < b)
                i++;
            else
                j++;
        }
        return result;    
    }
    
    /// <summary>
    /// Merges three index segments by entity id, returning a list of the entity ids found in all three segments. This is a
    /// union operation, and assumes that the three segments ar ordered by entity id.
    /// </summary>
    public static List<EntityId> MergeByEntityId(this IndexSegment setA, IndexSegment setB, IndexSegment setC)
    {
        // Entity ids are stored in the lower ulong of the key prefix
        var lowerA = setA.Lowers;
        var lowerB = setB.Lowers;
        var lowerC = setC.Lowers;
        
        // This is a merge join, we find the intersection of the two sets based on the E value
        List<EntityId> result = new();
        
        var i = 0;
        var j = 0;
        var k = 0;
        
        while (true)
        {
            if (i >= lowerA.Length || j >= lowerB.Length || k >= lowerC.Length)
                break;
            // Lop off the lower 8 bits as those are the flags
            var initialA = lowerA[i];
            var a = initialA >> 8;
            var b = lowerB[j] >> 8;
            var c = lowerC[k] >> 8;
            
            if (a == b && a == c)
            {
                result.Add(EntityId.From((initialA & 0xFF00000000000000) | ((initialA >> 8) & 0x0000FFFFFFFFFFFF)));
                i++;
                j++;
                k++;
            }
            else if (a < b || a < c)
                i++;
            else if (b < a || b < c)
                j++;
            else
                k++;
        }
        
        return result;
    }

    /// <summary>
    /// Merges four index segments by entity id, returning a list of the entity ids found in all four segments. This is a
    /// union operation, and assumes that the four segments ar ordered by entity id.
    /// </summary>
    public static List<EntityId> MergeByEntityId(this IndexSegment setA, IndexSegment setB, IndexSegment setC, IndexSegment setD)
    {
        var lowerA = setA.Lowers;
        var lowerB = setB.Lowers;
        var lowerC = setC.Lowers;
        var lowerD = setD.Lowers;
        
        List<EntityId> result = new();
        
        var i = 0;
        var j = 0;
        var k = 0;
        var l = 0;
        
        while (true)
        {
            if (i >= lowerA.Length || j >= lowerB.Length || k >= lowerC.Length || l >= lowerD.Length)
                break;
            
            var initialA = lowerA[i];
            var a = initialA >> 8;
            var b = lowerB[j] >> 8;
            var c = lowerC[k] >> 8;
            var d = lowerD[l] >> 8;
            
            if (a == b && a == c && a == d)
            {
                result.Add(EntityId.From((initialA & 0xFF00000000000000) | ((initialA >> 8) & 0x0000FFFFFFFFFFFF)));
                i++;
                j++;
                k++;
                l++;
            }
            else if (a < b || a < c || a < d)
                i++;
            else if (b < a || b < c || b < d)
                j++;
            else if (c < a || c < b || c < d)
                k++;
            else
                l++;
        }
        
        return result;
    }

    #region IDb Extensions using the above methods
    
    /// <summary>
    /// Finds all the entity ids that have both of the given attributes with the given values. Assumes that all the
    /// attributes are indexed or references.
    /// </summary>
    public static List<EntityId> Datoms<TValueA, TValueB>(this IDb db,
        (IWritableAttribute<TValueA> attributeA, TValueA valueA) pairA,
        (IWritableAttribute<TValueB> attributeB, TValueB valueB) pairB)
    {
        var setA = db.Datoms(pairA.attributeA, pairA.valueA);
        var setB = db.Datoms(pairB.attributeB, pairB.valueB);
        return setA.MergeByEntityId(setB);
    }
    
    /// <summary>
    /// Finds all the entity ids that have all three of the given attributes with the given values. Assumes that all
    /// the attributes are indexed or references.
    /// </summary>
    public static List<EntityId> Datoms<TValueA, TValueB, TValueC>(this IDb db,
        (IWritableAttribute<TValueA> attributeA, TValueA valueA) pairA,
        (IWritableAttribute<TValueB> attributeB, TValueB valueB) pairB,
        (IWritableAttribute<TValueC> attributeC, TValueC valueC) pairC)
    {
        var setA = db.Datoms(pairA.attributeA, pairA.valueA);
        var setB = db.Datoms(pairB.attributeB, pairB.valueB);
        var setC = db.Datoms(pairC.attributeC, pairC.valueC);
        return setA.MergeByEntityId(setB, setC);
    }
    
    /// <summary>
    /// Finds all the entity ids that have all four of the given attributes with the given values. Assumes that all
    /// the attributes are indexed or references.
    /// </summary>
    public static List<EntityId> Datoms<TValueA, TValueB, TValueC, TValueD>(this IDb db,
        (IWritableAttribute<TValueA> attributeA, TValueA valueA) pairA,
        (IWritableAttribute<TValueB> attributeB, TValueB valueB) pairB,
        (IWritableAttribute<TValueC> attributeC, TValueC valueC) pairC,
        (IWritableAttribute<TValueD> attributeD, TValueD valueD) pairD)
    {
        var setA = db.Datoms(pairA.attributeA, pairA.valueA);
        var setB = db.Datoms(pairB.attributeB, pairB.valueB);
        var setC = db.Datoms(pairC.attributeC, pairC.valueC);
        var setD = db.Datoms(pairD.attributeD, pairD.valueD);
        return setA.MergeByEntityId(setB, setC, setD);
    }

    #endregion
    
}
