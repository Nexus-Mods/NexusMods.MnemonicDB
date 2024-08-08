using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public class TuplePath(string ns, string name) : TupleAttribute<EntityId, ulong, string, string>(ValueTags.Reference, ValueTags.Ascii, ns, name)
{
    protected override (EntityId, string) FromLowLevel((ulong, string) value)
    {
        return (EntityId.From(value.Item1), value.Item2);
    }

    protected override (ulong, string) ToLowLevel((EntityId, string) value)
    {
        return (value.Item1.Value, value.Item2);
    }
}
