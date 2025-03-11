using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public class HashedBlobAttribute(string ns, string name) : ScalarAttribute<Memory<byte>, Memory<byte>, HashedBlobSerializer>(ns, name)
{
    protected override Memory<byte> ToLowLevel(Memory<byte> value)
    {
        return value;
    }

    protected override Memory<byte> FromLowLevel(Memory<byte> value, AttributeResolver resolver)
    {
        return value;
    }
}
