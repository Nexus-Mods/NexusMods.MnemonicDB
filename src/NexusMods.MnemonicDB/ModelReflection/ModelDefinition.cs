using System;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.ModelReflection;

/// <summary>
/// Definition of a model.
/// </summary>
/// <param name="Namespace"></param>
/// <param name="Parents"></param>
public record ModelDefinition(Type Type, Type[] Parents, IAttribute[] Attributes, ModelPropertyDefinition[] Properties)
{
    /// <summary>
    /// Name of the model.
    /// </summary>
    public string Name => Type.FullName ?? Type.Name;
}
