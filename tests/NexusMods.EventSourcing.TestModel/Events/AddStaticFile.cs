using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;
using NexusMods.EventSourcing.TestModel.Model.FileTypes;

namespace NexusMods.EventSourcing.TestModel.Events;

[EventId("93BDFE0C-E1EF-4572-B329-FD6C2CDBC8F6")]
public sealed record AddStaticFile(EntityId<Mod> ModId, EntityId<StaticFile> FileId, string Path, ulong Hash, ulong Size) : IEvent
{
    public void Apply<T>(T context) where T : IEventContext
    {
        IEntity.TypeAttribute.New(context, FileId);
        Mod._files.Add(context, ModId, FileId.Cast<AFile>());
        AFile._path.Set(context, FileId.Cast<AFile>(), Path);
        StaticFile._hash.Set(context, FileId, Hash);
        StaticFile._size.Set(context, FileId, Size);
    }

    public static EntityId<StaticFile> Create(ITransaction tx, EntityId<Mod> modId, string path, ulong hash, ulong size)
    {
        var fileId = EntityId<StaticFile>.NewId();
        var @event = new AddStaticFile(modId, fileId, path, hash, size);
        tx.Add(@event);
        return fileId;
    }
}
