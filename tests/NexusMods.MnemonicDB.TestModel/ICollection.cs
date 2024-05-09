using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.TestModel;

public interface ICollection : IModel
{
    public static class Attributes
    {
        private const string Namespace = "NexusMods.MnemonicDB.TestModel.Collection";
        public static readonly StringAttribute Name = new(Namespace, "Name");
        public static readonly ReferencesAttribute Mods = new(Namespace, "Mods");
        public static readonly ReferenceAttribute Loadout = new(Namespace, "Loadout");
    }

    [From(nameof(Attributes.Name))]
    public string Name { get; set; }

    [From(nameof(Attributes.Mods))]
    public IEnumerable<IModel> Mods { get; }

    [From(nameof(Attributes.Loadout))]
    public ILoadout Loadout { get; set; }
}
