using Newtonsoft.Json.Linq;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Tests.Resources.Rows;

public record ArchiveRow(string Name, Hash Hash, Size Size, string Type)
{
    public static ArchiveRow Parse(JToken token)
    {
        var stateType = ((string)token["State"]!["$type"]!).Split(',')[0];

        var size = Size.From(ulong.Parse((string)token["Size"]!));
        return new ArchiveRow((string)token["Name"]!, Helpers.HashFromBase64((string)token["Hash"]!), size, stateType);
    }
}
