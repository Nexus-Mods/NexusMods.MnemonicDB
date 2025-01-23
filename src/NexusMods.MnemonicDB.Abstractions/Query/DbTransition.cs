namespace NexusMods.MnemonicDB.Abstractions.Query;

public readonly record struct DbTransition(IDb? Previous, IDb? New)
{
    
}
