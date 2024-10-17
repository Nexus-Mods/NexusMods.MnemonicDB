using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public sealed class RelativePathAttribute(string ns, string name) :
    ScalarAttribute<RelativePath, string, Utf8InsensitiveSerializer>(ns, name)
{
    protected override string ToLowLevel(RelativePath value) => value.Path;

    protected override RelativePath FromLowLevel(string value, AttributeResolver resolver) => new(value);
}
