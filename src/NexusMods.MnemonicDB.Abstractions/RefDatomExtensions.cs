using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.MnemonicDB.Abstractions;

public static class RefDatomExtensions
{
    /// <summary>
    /// Builds an entity segment from the given enumerator.
    /// </summary>
    public static EntitySegment AsEntitySegment<TEnumerator>(this TEnumerator enumerator, IDb db, EntityId id) 
        where TEnumerator : IRefDatomEnumerator
    {
        using var builder = new IndexSegmentBuilder(db.AttributeCache);
        while (enumerator.MoveNext())
        {
            builder.AddCurrent(enumerator);
        }
        return builder.BuildEntitySegment(db, id);
    }
    
}
