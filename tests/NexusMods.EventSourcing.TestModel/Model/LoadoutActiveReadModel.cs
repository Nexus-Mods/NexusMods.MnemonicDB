using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;
using NexusMods.EventSourcing.TestModel.Model.Attributes;

namespace NexusMods.EventSourcing.TestModel.Model;

public class LoadoutActiveReadModel : AActiveReadModel<LoadoutActiveReadModel>
{
    public LoadoutActiveReadModel(IDb basisDb, EntityId id) : base(basisDb, id)
    {
    }

    [From<LoadoutAttributes.Name>]
    public string Name { get; set; } = string.Empty;
}
