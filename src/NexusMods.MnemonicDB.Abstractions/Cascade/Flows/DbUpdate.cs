namespace NexusMods.MnemonicDB.Abstractions.Cascade.Flows;

public enum UpdateType
{
    /// <summary>
    /// Default, uninitialized state
    /// </summary>
    None,
    
    /// <summary>
    /// Init, all values should be pulled via datoms from the Next db
    /// </summary>
    Init,
    
    /// <summary>
    /// Values should be pulled from the RecentDatoms on the Next db
    /// </summary>
    NextTx,
    
    /// <summary>
    /// Slow fallback, values in Prev should be retracted, new values in Next should be asserted
    /// </summary>
    RemoveAndAdd
}

public readonly record struct DbUpdate(IDb? Prev, IDb? Next, UpdateType UpdateType);
