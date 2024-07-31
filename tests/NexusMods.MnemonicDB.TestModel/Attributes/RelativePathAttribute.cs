using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public class RelativePathAttribute(string ns, string name) :
    ScalarAttribute<RelativePath, string>(ValueTags.Utf8Insensitive, ns, name)
{
    protected override string ToLowLevel(RelativePath value)
    {
        return value.Path;
    }

    protected override RelativePath FromLowLevel(string lowLevelType, ValueTags tags, RegistryId registryId)
    {
        return new RelativePath(lowLevelType);
    }
}
