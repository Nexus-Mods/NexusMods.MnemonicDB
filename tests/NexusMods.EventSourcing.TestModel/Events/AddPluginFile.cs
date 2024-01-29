using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.TestModel.Model;
using NexusMods.EventSourcing.TestModel.Model.FileTypes;

namespace NexusMods.EventSourcing.TestModel.Events;

[EventId("0E338937-404C-4596-930A-A37A2AB3506C")]
public sealed record AddPluginFile(EntityId<Mod> ModId, EntityId<PluginFile> FileId, string[] Plugins) : IEvent
{
    public void Apply<T>(T context) where T : IEventContext
    {
        IEntity.TypeAttribute.New(context, FileId);
        Mod._files.Add(context, ModId, FileId.Cast<AFile>());
        AFile._path.Set(context, FileId.Cast<AFile>(), "plugins.json");
        PluginFile._plugins.Set(context, FileId, Plugins);
    }

    public static EntityId<PluginFile> Create(ITransaction tx, EntityId<Mod> modId, string[] plugins)
    {
        var fileId = EntityId<PluginFile>.NewId();
        var @event = new AddPluginFile(modId, fileId, plugins);
        tx.Add(@event);
        return fileId;
    }
}
