using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model;

[Entity("0FFCC422-7281-44C4-A4E6-32ACE31C00AF")]
public class ArchiveEntry(IEntityContext context, EntityId<ArchiveEntry> id) : AEntity(context, id)
{
    public string Path => _path.Get(this);
    internal static readonly ScalarAttribute<ArchiveEntry, string> _path = new(nameof(Path));

    public string Foo => _foo.Get(this);
    internal static readonly ScalarAttribute<ArchiveEntry, string> _foo = new(nameof(Foo));


    [Indexed("495CA9CB-F803-4577-853E-AA9AC196D3EA")]
    public ulong Size => _size.Get(this);
    internal static readonly ULongAttribute<ArchiveEntry> _size = new(nameof(Size));


    [Indexed("9FD7C60A-B71E-4B3B-BB68-965312553D69")]
    public ulong Hash => _hash.Get(this);
    public static readonly ULongAttribute<ArchiveEntry> _hash = new(nameof(Hash));

    public Archive Archive => _archive.Get(this);
    internal static readonly EntityAttributeDefinition<ArchiveEntry, Archive> _archive = new(nameof(Archive));
}
