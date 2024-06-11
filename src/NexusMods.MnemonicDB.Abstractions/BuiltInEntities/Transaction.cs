using NexusMods.MnemonicDB.Abstractions.Models;
using TimestampAttribute = NexusMods.MnemonicDB.Abstractions.Attributes.TimestampAttribute;

namespace NexusMods.MnemonicDB.Abstractions.BuiltInEntities;

/// <summary>
/// Metadata for a transaction, transactions are themselves entities in the database, and
/// so can have models like any other entity.
/// </summary>
public partial class Transaction : IModelDefinition
{
    private const string Namespace = "NexusMods.MnemonicDB.Transaction";

    /// <summary>
    /// The timestamp when the transaction was committed.
    /// </summary>
    public static readonly TimestampAttribute Timestamp = new(Namespace, "Timestamp");
}
