namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// The authoritative source of attribute definitions is the database itself, this class provides a cache
/// of these definitions for use in the rest of the code routines
/// </summary>
public sealed class AttributeCache
{
    /// <summary>
    /// Resets the cache, causing it to re-query the database for the latest definitions.
    /// </summary>
    /// <param name="idb"></param>
    public void Reset(IDb idb)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Returns true if the attribute is a reference attribute.
    /// </summary>
    public bool IsReference(AttributeId attrId)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Returns true if the attribute is indexed.
    /// </summary>
    public bool IsIndexed(AttributeId attrId)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Returns true if the attribute is `NoHistory`.
    /// </summary>
    public bool IsNoHistory(AttributeId attrId)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Returns true if the attribute cardinality is many.
    /// </summary>
    public bool IsCardinalityMany(AttributeId attrId)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Get the AttributeId (DB attribute id) for the given attribute name
    /// </summary>
    public AttributeId GetAttributeId(Symbol attribute)
    {
        throw new System.NotImplementedException();
    }
}
