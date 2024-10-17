using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public sealed class UriAttribute(string ns, string name) : ScalarAttribute<Uri, string, Utf8Serializer>(ns, name)
{
    protected override string ToLowLevel(Uri value) 
        => value.ToString();

    protected override Uri FromLowLevel(string lowLevelValue, AttributeResolver resolver) 
        => new(lowLevelValue);
}
