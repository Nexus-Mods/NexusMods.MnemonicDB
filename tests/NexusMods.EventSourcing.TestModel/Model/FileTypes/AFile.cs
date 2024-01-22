using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model.FileTypes;

[Entity("34DA0FCC-60A7-4CB9-89D8-9A9C5045C4EB", 0)]
public abstract class AFile<TOwner>(IEntityContext context, EntityId id) : AEntity(context, id) where TOwner : AEntity
{
    /// <summary>
    /// The output path of the file
    /// </summary>
    public string Path => _path.Get(this);
    internal static readonly ScalarAttribute<AFile<TOwner>, string> _path = new(nameof(Path));

}
