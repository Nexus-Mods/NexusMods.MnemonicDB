using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public class Int3Attribute(string ns, string name) : TupleAttribute<int, int, int, int, int, int>(ValueTags.Int32, ValueTags.Int32, ValueTags.Int32, ns, name)
{
    protected override (int, int, int) FromLowLevel((int, int, int) value)
    {
        return value;
    }

    protected override (int, int, int) ToLowLevel((int, int, int) value)
    {
        return value;
    }
}
