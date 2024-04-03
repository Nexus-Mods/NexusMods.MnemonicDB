﻿using NexusMods.MneumonicDB.Abstractions;

namespace NexusMods.MneumonicDB.TestModel.ComplexModel.Attributes;

public class LoadoutAttributes
{
    /// <summary>
    ///     Name of the loadout
    /// </summary>
    public class Name : Attribute<Name, string>;

    /// <summary>
    ///     Incremented updated whenever any aspect of the loadout is changed
    /// </summary>
    public class UpdatedAt : Attribute<UpdatedAt, TxId>;
}
