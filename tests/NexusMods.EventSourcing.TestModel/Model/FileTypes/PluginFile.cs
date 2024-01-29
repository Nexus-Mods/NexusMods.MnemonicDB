using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model.FileTypes;

[Entity("05B2C299-F5C2-4DD4-9616-9D6DF1524D62")]
public class PluginFile(IEntityContext context, EntityId<PluginFile> id) : AFile(context, id)
{
    public string[] Plugins => _plugins.Get(this);
    internal static readonly ScalarAttribute<PluginFile, string[]> _plugins = new(nameof(Plugins));

}
