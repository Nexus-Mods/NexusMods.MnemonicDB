using NexusMods.MneumonicDB.Abstractions;

namespace NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes;

public class CollectionAttributes
{
    public class Name : Attribute<Name, string>;

    public class Mods() : Attribute<Mods, EntityId>(multiValued: true);

    public class LoadoutId : Attribute<LoadoutId, EntityId>;
}
