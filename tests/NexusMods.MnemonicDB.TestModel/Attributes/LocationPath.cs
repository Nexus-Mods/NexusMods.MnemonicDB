using System.Diagnostics;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public sealed class LocationPath(string ns, string name) : ScalarAttribute<(LocationId, RelativePath), (ushort, string), Tuple2_UShort_Utf8I_Serializer>(ns, name)
{
    protected override (ushort, string) ToLowLevel((LocationId, RelativePath) value) 
        => (value.Item1.Value, value.Item2.Path);

    protected override (LocationId, RelativePath) FromLowLevel((ushort, string) value, AttributeResolver resolver)
    {
        // NOTE(erri120): Stored data should be sanitized already.
        Debug.Assert(PathHelpers.IsSanitized(value.Item2, OSInformation.Shared, isRelative: true));
        var path = RelativePath.CreateUnsafe(value.Item2);
        return (LocationId.From(value.Item1), path);
    }
}
