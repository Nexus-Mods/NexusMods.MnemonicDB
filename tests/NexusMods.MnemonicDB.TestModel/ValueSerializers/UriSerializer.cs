using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Serializers;

namespace NexusMods.MnemonicDB.TestModel.ValueSerializers;

public class UriSerializer() : AUtf8Serializer<Uri>(false)
{
    public override Symbol UniqueId => Symbol.Intern<UriSerializer>();

    protected override ReadOnlySpan<char> ToSpan(Uri value)
    {
        return value.ToString();
    }

    protected override Uri FromSpan(ReadOnlySpan<char> span)
    {
        return new Uri(span.ToString());
    }
}
