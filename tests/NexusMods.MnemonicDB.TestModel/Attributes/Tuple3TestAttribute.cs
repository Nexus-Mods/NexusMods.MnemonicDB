using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public sealed class Tuple3TestAttribute(string ns, string name) : ScalarAttribute<(EntityId, ushort, RelativePath), (EntityId, ushort, string)>(ValueTag.Tuple3_Ref_UShort_Utf8I, ns, name)
{
    protected override (EntityId, ushort, string) ToLowLevel((EntityId, ushort, RelativePath) value) 
        => (value.Item1, value.Item2, value.Item3.Path);

    protected override (EntityId, ushort, RelativePath) FromLowLevel((EntityId, ushort, string) value, AttributeResolver resolver) 
        => (value.Item1, value.Item2, new(value.Item3));
}
