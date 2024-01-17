using System.Collections.ObjectModel;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.TestModel.Model;

[Entity("10EB47FB-739E-46AF-8A77-BEF8EE084093", 0)]
public class Archive(IEntityContext context, EntityId<Archive> id) : AEntity<Archive>(context, id)
{

    [Indexed("6163F4E6-78FB-4C80-8602-AE35B4FB4B20")]
    public ulong Size => _size.Get(this);
    internal static readonly ScalarAttribute<Archive, ulong> _size = new(nameof(Size));

    public ReadOnlyObservableCollection<ArchiveEntry> Entries => _entries.Get(this);
    internal static readonly MultiEntityAttributeDefinition<Archive, ArchiveEntry> _entries = new(nameof(Entries));
}
