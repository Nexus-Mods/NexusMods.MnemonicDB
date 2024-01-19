using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.EventSourcing.TestModel.Events;

[EventId("F94A18CF-B305-49B6-9A8B-38A5AA23EC0F")]
public sealed record AddArchive(EntityId<Archive> Id, ulong ArchiveHash, ulong ArchiveSize,
    EntityId<ArchiveEntry>[] EntityIds, string[] EntryPaths, ulong[] EntryHashes, ulong[] EntrySizes) : IEvent
{
    public void Apply<T>(T context) where T : IEventContext
    {
        IEntity.TypeAttribute.New(context, Id);
        Archive._hash.Set(context, Id, ArchiveHash);
        Archive._size.Set(context, Id, ArchiveSize);

        for (var i = 0; i < EntityIds.Length; i++)
        {
            var entryId = EntityIds[i];
            IEntity.TypeAttribute.New(context, entryId);
            ArchiveEntry._path.Set(context, entryId, EntryPaths[i]);
            ArchiveEntry._hash.Set(context, entryId, EntryHashes[i]);
            ArchiveEntry._size.Set(context, entryId, EntrySizes[i]);
            Archive._entries.Add(context, Id, entryId);
        }
    }

    public static EntityId<Archive> Create(ITransaction tx, ulong ArchiveHash, ulong ArchiveSize, params (string Path, ulong Hash, ulong Size)[] Contents)
    {
        var id = EntityId<Archive>.NewId();

        tx.Add(new AddArchive(id, ArchiveHash, ArchiveSize,
            Contents.Select(_ => EntityId<ArchiveEntry>.NewId()).ToArray(),
            Contents.Select(c => c.Path).ToArray(),
            Contents.Select(c => c.Hash).ToArray(),
            Contents.Select(c => c.Size).ToArray()));
        return id;
    }
}
