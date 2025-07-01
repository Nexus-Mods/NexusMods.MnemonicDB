namespace NexusMods.MnemonicDB.Abstractions;

public record ModelTableDefinition(string Name, IAttribute PrimaryAttribute, IAttribute[] Attributes);
