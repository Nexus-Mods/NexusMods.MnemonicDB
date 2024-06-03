using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// A meta attribute the expresses a backwards reference some other
/// model has
/// </summary>
public class BackReferenceAttribute<TOtherModel>(ReferenceAttribute referenceAttribute)
where TOtherModel : IModelDefinition
{
}
