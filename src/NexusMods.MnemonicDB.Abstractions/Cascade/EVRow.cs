namespace NexusMods.MnemonicDB.Abstractions.Cascade;

/// <summary>
/// A result row of just an entity id and value 
/// </summary>
public readonly record struct EVRow<T>(EntityId Id, T Value)
{
    
}
