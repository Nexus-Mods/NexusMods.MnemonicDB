using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments.Columns;

public sealed class EntityIdColumn : ASimpleColumn<EntityId>
{
    public static readonly EntityIdColumn Instance = new();
    public override EntityId GetValue(in KeyPrefix prefix)
    {
        return prefix.E;
    }
}
