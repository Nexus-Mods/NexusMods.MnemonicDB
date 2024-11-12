using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.TestModel.Attributes;

namespace NexusMods.MnemonicDB.LargeTestModel.Models;

public partial class Archive : IModelDefinition
{
    private const string Namespace = "LargeModel.Archive";
    
    public static readonly StringAttribute Name = new(Namespace, "Name") { IsIndexed = true };
    public static readonly HashAttribute Hash = new(Namespace, "Hash") { IsIndexed = true };
    public static readonly SizeAttribute Size = new(Namespace, "Size") { IsIndexed = true };
}
