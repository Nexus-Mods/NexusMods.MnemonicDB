using NexusMods.MneumonicDB.Abstractions;

namespace NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes;

public class CollectionAttributes
{
    public class Name : ScalarAttribute<Name, string>;

    public class Mods() : ScalarAttribute<Mods, EntityId>(multiValued: true);

    public class LoadoutId : ScalarAttribute<LoadoutId, EntityId>;
}
