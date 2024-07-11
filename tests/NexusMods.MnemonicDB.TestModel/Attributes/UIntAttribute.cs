using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public class UIntAttribute(string ns, string name) : ScalarAttribute<uint, uint>(ValueTags.UInt32, ns, name)
{
    protected override uint ToLowLevel(uint value)
    {
        return value;
    }

    protected override uint FromLowLevel(uint value, ValueTags tags)
    {
        return value;
    }

}
