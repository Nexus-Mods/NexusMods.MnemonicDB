using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public class Int3Attribute(string ns, string name) : TupleAttribute<int, int, int, int, string, string>(ValueTags.Int32, ValueTags.Int32, ValueTags.Ascii, ns, name)
{
    protected override (int, int, string) FromLowLevel((int, int, string) value)
    {
        return value;
    }

    protected override (int, int, string) ToLowLevel((int, int, string) value)
    {
        return value;
    }
}
