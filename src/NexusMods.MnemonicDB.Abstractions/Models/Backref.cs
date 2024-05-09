using System;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// Defines a backref lookup on a model
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class Backref(string otherProperty) : Attribute
{

}
