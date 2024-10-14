namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// A marker attribute for functions that are internal to the MnemonicDB, used mostly for schema updates
/// and other bulk operations that are started via the TX Queue but are exposed to users through non transaction APIs
/// </summary>
public interface IInternalTxFunction;
