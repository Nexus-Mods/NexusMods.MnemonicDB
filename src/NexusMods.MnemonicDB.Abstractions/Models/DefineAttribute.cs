using System;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// Define a property on a model as being a defined attribute in the database.
/// The type will be inferred from the property type, and use the simplest direct
/// mapping, so a string will become a UTF-8 case sensitive string, and an int
/// will become a 32-bit signed integer.
///
/// If the dbName parameter is provided, it will be used as the name of the database
/// attribute, otherwise the property name will be used.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DefineAttribute(string dbName = "") : Attribute
{
    /// <summary>
    /// The name of the database attribute.
    /// </summary>
    public string Name => dbName;
}
