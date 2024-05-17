using System;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// Denotes that this model includes the attributes of another model
/// </summary>
/// <typeparam name="T"></typeparam>
[AttributeUsage(AttributeTargets.Class)]
public class Include<T> : Attribute
where T : IModelDefinition
{
    /// <summary>
    /// The type of the other model
    /// </summary>
    public Type Other => typeof(T);

}
