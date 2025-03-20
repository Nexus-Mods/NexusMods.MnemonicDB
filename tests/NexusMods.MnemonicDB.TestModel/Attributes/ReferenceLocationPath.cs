using System.Diagnostics;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public sealed class ReferenceLocationPath(string ns, string name) : ScalarAttribute<(EntityId, LocationId, RelativePath), (EntityId, ushort, string), Tuple3_Ref_UShort_Utf8I_Serializer>(ns, name)
{
    protected override (EntityId, ushort, string) ToLowLevel((EntityId, LocationId, RelativePath) value)
        => (value.Item1, value.Item2.Value, value.Item3.Path);

    protected override (EntityId, LocationId, RelativePath) FromLowLevel((EntityId, ushort, string) value, AttributeResolver resolver)
    {
        // NOTE(erri120): Stored data should be sanitized already.
        Debug.Assert(PathHelpers.IsSanitized(value.Item3, OSInformation.Shared, isRelative: true));
        var path = RelativePath.CreateUnsafe(value.Item3);
        return (value.Item1, LocationId.From(value.Item2), path);
    }
}
