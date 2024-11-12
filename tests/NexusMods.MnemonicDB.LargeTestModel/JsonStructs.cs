using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.LargeTestModel;

public class Root
{
    public required List<JsonArchive> Archives { get; init; }
    public required Dictionary<string, bool> Mods { get; init; }
    public required List<Directive> Directives { get; init; }
}

public class JsonArchive
{
    public required ulong Hash { get; init; }
    public required Size Size { get; init; }
    public required string Name { get; init; }
}

public class Directive
{
    public required string To { get; init; }
    public required ulong Hash { get; init; }
    public required Size Size { get; init; }
    public string? Mod { get; init; }
}
