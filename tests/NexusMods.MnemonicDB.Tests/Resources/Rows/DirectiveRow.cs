using Newtonsoft.Json.Linq;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Tests.Resources.Rows;

public record DirectiveRow(RelativePath To, Hash ArchiveHash, RelativePath? ArchivePath, Size Size, Hash Hash)
{
    public static DirectiveRow? Parse(JToken token)
    {
        var to = RelativePath.FromUnsanitizedInput((string)token["To"]!);
        var hashPath = token["ArchiveHashPath"];
        if (hashPath == null)
            return null;
        var archiveHash = Helpers.HashFromBase64((string)hashPath[0]!);

        RelativePath? archivePath = null;
        var pathArray = (JArray)hashPath;
        if (pathArray.Count >= 2)
            archivePath = RelativePath.FromUnsanitizedInput((string)pathArray[0]!);

        var size = Size.From(ulong.Parse((string)token["Size"]!));
        var hash = Helpers.HashFromBase64((string)token["Hash"]!);

        return new DirectiveRow(to, archiveHash, archivePath, size, hash);
    }

}
