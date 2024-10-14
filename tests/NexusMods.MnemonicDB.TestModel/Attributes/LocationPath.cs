using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public sealed class LocationPath(string ns, string name) : ScalarAttribute<(LocationId, RelativePath), (ushort, string)>(ValueTag.Tuple2_UShort_Utf8I, ns, name)
{
    protected override (ushort, string) ToLowLevel((LocationId, RelativePath) value) 
        => (value.Item1.Value, value.Item2.Path);

    protected override (LocationId, RelativePath) FromLowLevel((ushort, string) value, AttributeResolver resolver) 
        => (LocationId.From(value.Item1), new(value.Item2));
}
