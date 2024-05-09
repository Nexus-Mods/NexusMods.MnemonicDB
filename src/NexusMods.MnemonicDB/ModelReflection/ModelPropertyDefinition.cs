using System;
using System.Reflection;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.ModelReflection;

/// <summary>
/// Definition of a model member.
/// </summary>
public record ModelPropertyDefinition(
    string Name,
    PropertyInfo Property,
    FieldInfo AttributeField,
    IAttribute DbAttribute)
{

}
