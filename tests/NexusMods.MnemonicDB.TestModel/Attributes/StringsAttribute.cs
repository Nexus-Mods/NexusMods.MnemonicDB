using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public class StringsAttribute(string ns, string name) : CollectionAttribute<string, string>(ValueTags.Utf8, ns, name)
{
    protected override string ToLowLevel(string value)
    {
        return value;
    }

    protected override string FromLowLevel(string value, ValueTags tag, RegistryId registryId)
    {
        return value;
    }
}
