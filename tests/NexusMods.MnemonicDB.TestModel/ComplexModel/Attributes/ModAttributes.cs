using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.TestModel.ComplexModel.Attributes;

public class ModAttributes
{
    /// <summary>
    ///     The Name of the mod
    /// </summary>
    public class Name() : Attribute<Name, string>(isIndexed: true);

    /// <summary>
    ///     The download source of the mod
    /// </summary>
    public class Source : Attribute<Source, Uri>;

    /// <summary>
    ///     The loadout that the mod is part of
    /// </summary>
    public class LoadoutId : Attribute<LoadoutId, EntityId>;
}
