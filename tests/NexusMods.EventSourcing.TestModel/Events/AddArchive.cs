using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.TestModel.Events;

[EventId("F94A18CF-B305-49B6-9A8B-38A5AA23EC0F")]
public sealed record AddArchive(EntityId<Archive> Id, ulong ArchiveHash, ulong ArchiveSize, (EntityId<ArchiveEntry> Id, string Path, ulong Hash, ulong Size)[] Contents) : IEvent
{
    public void Apply<T>(T context) where T : IEventContext
    {
        IEntity.TypeAttribute.New(context, Id);
        Archive._hash.Set(context, Id, ArchiveHash);
        Archive._size.Set(context, Id, ArchiveSize);

        foreach (var (id, path, hash, size) in Contents)
        {
            IEntity.TypeAttribute.New(context, id);
            ArchiveEntry._path.Set(context, id, path);
            ArchiveEntry._hash.Set(context, id, hash);
            ArchiveEntry._size.Set(context, id, size);
            Archive._entries.Add(context, Id, id);
        }
    }

    public static EntityId<Archive> Create(ITransaction tx, ulong ArchiveHash, ulong ArchiveSize, params (string Path, ulong Hash, ulong Size)[] Contents)
    {
        var id = EntityId<Archive>.NewId();
        tx.Add(new AddArchive(id, ArchiveHash, ArchiveSize, Contents.Select(c =>
            (EntityId<ArchiveEntry>.NewId(), c.Path, c.Hash, c.Size))
            .ToArray()));
        return id;
    }
}
