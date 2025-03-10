using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments.Columns;

public sealed class AttributeIdColumn : ASimpleColumn<AttributeId>
{
    /// <summary>
    /// The singleton instance of the <see cref="AttributeIdColumn"/>.
    /// </summary>
    public static readonly AttributeIdColumn Instance = new();
    public override AttributeId GetValue(in KeyPrefix prefix) => prefix.A;
}
