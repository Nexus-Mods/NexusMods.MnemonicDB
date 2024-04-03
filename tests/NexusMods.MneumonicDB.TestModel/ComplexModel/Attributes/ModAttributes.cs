﻿using NexusMods.MneumonicDB.Abstractions;

namespace NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes;

public class ModAttributes
{
    /// <summary>
    ///     The Name of the mod
    /// </summary>
    public class Name : Attribute<Name, string>;

    /// <summary>
    ///     The download source of the mod
    /// </summary>
    public class Source : Attribute<Source, Uri>;

    /// <summary>
    ///     The loadout that the mod is part of
    /// </summary>
    public class LoadoutId : Attribute<LoadoutId, EntityId>;
}
