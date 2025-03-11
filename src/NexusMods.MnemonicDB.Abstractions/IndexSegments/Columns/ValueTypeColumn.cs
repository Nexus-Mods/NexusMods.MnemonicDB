using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Internals;

namespace NexusMods.MnemonicDB.Abstractions.IndexSegments.Columns;

public class ValueTypeColumn : ASimpleColumn<ValueTag>
{
    public static readonly ValueTypeColumn Instance = new();
    
    public override ValueTag GetValue(in KeyPrefix prefix)
    {
        return prefix.ValueTag;
    }
}
