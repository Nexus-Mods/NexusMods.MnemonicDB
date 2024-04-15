using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that represents a scalar value, where there is a 1:1 ratio between the attribute and the value.
/// </summary>
public abstract class ScalarAttribute<TValue, TLowLevel>(ValueTags tag, string ns, string name) :
    Attribute<TValue, TLowLevel>(tag, ns, name)
{
    /// <summary>
    ///   Gets the value of the attribute from the entity.
    /// </summary>
    public TValue Get(IEntity entity)
    {
        return entity.Db.Get(entity.Id, this);
    }
}
