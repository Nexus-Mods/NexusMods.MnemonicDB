using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Serializers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel.ValueSerializers;

public class RelativePathSerializer() : AUtf8Serializer<RelativePath>(false)
{
    public override Symbol UniqueId => Symbol.Intern<RelativePathSerializer>();
    protected override ReadOnlySpan<char> ToSpan(RelativePath value)
    {
        return value.Path;
    }

    protected override RelativePath FromSpan(ReadOnlySpan<char> span)
    {
        return new RelativePath(span.ToString());
    }
}
