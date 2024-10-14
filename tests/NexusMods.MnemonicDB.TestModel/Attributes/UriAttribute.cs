using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public sealed class UriAttribute(string ns, string name) : ScalarAttribute<Uri, string>(ValueTag.Utf8, ns, name)
{
    protected override string ToLowLevel(Uri value) 
        => value.ToString();

    protected override Uri FromLowLevel(string lowLevelValue, AttributeResolver resolver) 
        => new(lowLevelValue);
}
