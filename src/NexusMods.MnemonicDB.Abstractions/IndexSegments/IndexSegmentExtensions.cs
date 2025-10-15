using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.Traits;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments;

/// <summary>
/// Extensions for working with index segments
/// </summary>
public static class IndexSegmentExtensions
{
    
    #region IDb Extensions using the above methods
    
    /// <summary>
    /// Finds all the entity ids that have both of the given attributes with the given values. Assumes that all the
    /// attributes are indexed or references.
    /// </summary>
    public static List<EntityId> Datoms<TValueA, TValueB>(this IDb db,
        (IWritableAttribute<TValueA> attributeA, TValueA valueA) pairA,
        (IWritableAttribute<TValueB> attributeB, TValueB valueB) pairB)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    #endregion
    
}
