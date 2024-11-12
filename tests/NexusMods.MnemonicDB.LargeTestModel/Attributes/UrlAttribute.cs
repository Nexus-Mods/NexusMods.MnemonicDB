using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.LargeTestModel.Attributes;

/// <summary>
/// An attribute that represents a URL
/// </summary>
/// <param name="ns"></param>
/// <param name="name"></param>
public class UrlAttribute(string ns, string name) : ScalarAttribute<Uri, string, Utf8Serializer>(ns, name)
{
    protected override string ToLowLevel(Uri value) => value.ToString();
    protected override Uri FromLowLevel(string value, AttributeResolver resolver) => new(value);
}
