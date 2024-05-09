using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.TestModel;

public interface ILoadout : IModel
{
    public static class Attributes
    {
        public const string Namespace = "NexusMods.MnemonicDB.TestModel.Loadout";
        public static readonly StringAttribute Name = new(Namespace, "Name");
    }

    [From(nameof(Attributes.Name))]
    public string Name { get; set; }

    [Backref(nameof(IMod.Attributes.LoadoutId))]
    public IEnumerable<IMod> Mods { get; }
}
