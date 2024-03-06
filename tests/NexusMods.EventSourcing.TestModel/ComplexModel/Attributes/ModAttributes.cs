using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;

public class ModAttributes
{
    /// <summary>
    /// The Name of the mod
    /// </summary>
    public class Name : ScalarAttribute<Name, string>;

    /// <summary>
    /// The download source of the mod
    /// </summary>
    public class Source : ScalarAttribute<Source, Uri>;

    /// <summary>
    /// The loadout that the mod is part of
    /// </summary>
    public class LoadoutId : ScalarAttribute<LoadoutId, EntityId>;
}
