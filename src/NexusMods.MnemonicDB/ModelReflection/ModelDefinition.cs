namespace NexusMods.MnemonicDB.ModelReflection;

/// <summary>
/// Definition of a model.
/// </summary>
/// <param name="Namespace"></param>
/// <param name="Parents"></param>
public record ModelDefinition(string Namespace, ModelDefinition[] Parents);
