using System;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// Define a property on a model that should be indexed in the database.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IndexAttribute : Attribute
{

}
