using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// A meta attribute the expresses a backwards reference some other
/// model has
/// </summary>
public class BackReferenceAttribute<TOtherModel> where TOtherModel : IModelDefinition
{
    /// <summary>
    /// A meta attribute the expresses a backwards reference some other
    /// model has via a ReferenceAttribute
    /// </summary>
    public BackReferenceAttribute(ReferenceAttribute referenceAttribute)
    {
    }
    
    /// <summary>
    /// A meta attribute the expresses a backwards reference some other model has via a ReferencesAttribute
    /// </summary>
    public BackReferenceAttribute(ReferencesAttribute<TOtherModel> referencesAttribute)
    {
    }
}
