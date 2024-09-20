using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public class UriAttribute(string ns, string name) : ScalarAttribute<Uri, string>(ValueTags.Utf8, ns, name)
{
    protected override string ToLowLevel(Uri value)
    {
        return value.ToString();
    }

    protected override Uri FromLowLevel(string lowLevelValue, ValueTags tags, AttributeResolver resolver)
    {
        return new Uri(lowLevelValue);
    }
}
