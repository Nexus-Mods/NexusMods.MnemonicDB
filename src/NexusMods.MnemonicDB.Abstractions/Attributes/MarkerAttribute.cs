using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that doesn't have a value, but is used to dispatch logic or to mark an entity
/// as being of a certain type.
/// </summary>
/// <param name="ns"></param>
/// <param name="name"></param>
public class MarkerAttribute(string ns, string name) : Attribute<Null, Null>(ValueTags.Null, ns, name)
{
    /// <inheritdoc />
    protected override Null ToLowLevel(Null value)
    {
        return value;
    }

    /// <summary>
    /// Returns true if the entity contains the attribute.
    /// </summary>
    public bool Contains(IHasIdAndIndexSegment entity)
    {
        var segment = entity.IndexSegment;
        var dbId = Cache[segment.RegistryId.Value];
        for (var i = 0; i < segment.Count; i++)
        {
            var datom = segment[i];
            if (datom.A != dbId) continue;
            return true;
        }
        return false;
    }
}
