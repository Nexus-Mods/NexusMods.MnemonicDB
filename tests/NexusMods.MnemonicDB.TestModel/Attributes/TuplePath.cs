using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public sealed class TuplePath(string ns, string name) : ScalarAttribute<(ushort, RelativePath), (ushort, string)>(ValueTag.Tuple2_UShort_Utf8I, ns, name)
{
    protected override (ushort, string) ToLowLevel((ushort, RelativePath) value) 
        => (value.Item1, value.Item2.Path);

    protected override (ushort, RelativePath) FromLowLevel((ushort, string) value, AttributeResolver resolver) 
        => (value.Item1, new(value.Item2));
}
