using NexusMods.Cascade;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Static members for query support. Not often used directly by user code.
/// </summary>
public static class QueryInlets
{
    /// <summary>
    /// The inlet for database queries. This same inlet is used for connection and db queries
    /// </summary>
    public static readonly Inlet<DbTransition> Db = new();
}
