using System;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// Declares that a property is backed by a specific MnemonicDB attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class From(string nameFrom) : Attribute
{

}
