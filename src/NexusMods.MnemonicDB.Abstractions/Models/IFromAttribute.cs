using System;

namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
///     Base interface for all from attributes.
/// </summary>
public interface IFromAttribute
{
    /// <summary>
    ///     The attribute tag of this from attribute.
    /// </summary>
    public Type AttributeType { get; }
}
