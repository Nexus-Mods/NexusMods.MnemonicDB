namespace NexusMods.MnemonicDB.Abstractions;

public enum ScanResultType
{
    /// <summary>
    /// No changes to the datom
    /// </summary>
    None,
    /// <summary>
    /// The datom has changed, and should be updated
    /// </summary>
    Update,
    /// <summary>
    /// The datoms should be deleted
    /// </summary>
    Delete
}
