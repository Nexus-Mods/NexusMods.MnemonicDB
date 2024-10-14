using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public sealed class ReferenceLocationPath(string ns, string name) : ScalarAttribute<(EntityId, LocationId, RelativePath), (EntityId, ushort, string)>(ValueTag.Tuple3_Ref_UShort_Utf8I, ns, name)
{
    protected override (EntityId, ushort, string) ToLowLevel((EntityId, LocationId, RelativePath) value) 
        => (value.Item1, value.Item2.Value, value.Item3.Path);

    protected override (EntityId, LocationId, RelativePath) FromLowLevel((EntityId, ushort, string) value, AttributeResolver resolver) 
        => (value.Item1, LocationId.From(value.Item2), new(value.Item3));
}
