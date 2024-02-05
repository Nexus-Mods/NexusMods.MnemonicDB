using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model;

[Entity("05E5482D-CB2F-48AE-BE66-902B4B807A44")]
public class Mod
{
    /// <summary>
    /// The name of the mod
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The description of the mod
    /// </summary>
    public required string Description { get; init; }
}
