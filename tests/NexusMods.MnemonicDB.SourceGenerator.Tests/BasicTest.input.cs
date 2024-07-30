using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.SourceGenerator.Tests;

public partial class MyModel : IModelDefinition
{
    public static readonly StringAttribute Name = new("MyNamespace", nameof(Name));
}
