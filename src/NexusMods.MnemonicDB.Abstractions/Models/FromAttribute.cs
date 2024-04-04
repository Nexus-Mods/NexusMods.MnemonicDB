using System;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
///     Marks a property as being derived from an attribute.
/// </summary>
/// <typeparam name="TAttribute"></typeparam>
[AttributeUsage(AttributeTargets.Property)]
public class FromAttribute<TAttribute> : Attribute, IFromAttribute
    where TAttribute : IAttribute
{
    /// <summary>
    ///     Gets the type of the attribute.
    /// </summary>
    public Type AttributeType => typeof(TAttribute);
}
