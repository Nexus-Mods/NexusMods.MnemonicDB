using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;

namespace NexusMods.MnemonicDB.TestModel;

public partial class File : IModelDefinition
{
    private const string Namespace = "NexusMods.MnemonicDB.TestModel.File";
    public static readonly RelativePathAttribute Path = new(Namespace, nameof(Path)) {IsIndexed = false};
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash)) {IsIndexed = true};
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));
    public static readonly ReferenceAttribute<Mod> Mod = new(Namespace, nameof(Mod));

    /// <summary>
    /// A combination of the loadout 
    /// </summary>
    public static readonly TuplePath TuplePath = new(Namespace, nameof(TuplePath)) {IsIndexed = true, IsOptional = true};
    
    /// <summary>
    /// Tuple3 test
    /// </summary>
    public static readonly Int3Attribute TupleTest = new(Namespace, nameof(TupleTest)) { IsIndexed = true, IsOptional = true};
}
