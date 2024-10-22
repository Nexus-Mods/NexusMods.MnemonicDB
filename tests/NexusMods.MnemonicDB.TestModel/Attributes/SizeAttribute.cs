using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public sealed class SizeAttribute(string ns, string name) : ScalarAttribute<Size, ulong, UInt64Serializer>(ns, name) {
    protected override ulong ToLowLevel(Size value) => value.Value;

    protected override Size FromLowLevel(ulong value, AttributeResolver resolver)
        => Size.From(value);
}
