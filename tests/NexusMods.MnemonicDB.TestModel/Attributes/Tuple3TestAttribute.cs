using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public class Tuple3TestAttribute(string ns, string name) : TupleAttribute<EntityId, ulong, int, int, string, string>(ValueTags.Reference, ValueTags.Int32, ValueTags.Ascii, ns, name)
{
    protected override (EntityId, int, string) FromLowLevel((ulong, int, string) value)
    {
        return (EntityId.From(value.Item1), value.Item2, value.Item3);
    }

    protected override (ulong, int, string) ToLowLevel((EntityId, int, string) value)
    {
        return (value.Item1.Value, value.Item2, value.Item3);
    }
}
