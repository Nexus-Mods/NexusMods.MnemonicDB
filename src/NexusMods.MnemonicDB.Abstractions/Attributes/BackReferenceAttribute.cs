using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Models;
// ReSharper disable UnusedTypeParameter
#pragma warning disable CS9113 // Parameter is unread.

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// A meta attribute the expresses a backwards reference some other
/// model has
/// </summary>
[PublicAPI]
public sealed class BackReferenceAttribute<TOtherModel>(ReferenceAttribute referenceAttribute) where TOtherModel : IModelDefinition { }
