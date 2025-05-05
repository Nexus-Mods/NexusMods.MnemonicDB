using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.SourceGenerator.Tests;

public partial class IncludesTestModel1 : IModelDefinition
{
    public static readonly StringAttribute Name = new("MyNamespace1", nameof(Name));
}

[Include<IncludesTestModel1>]
public partial class IncludesTestModel2 : IModelDefinition
{
    public static readonly StringAttribute Name = new("MyNamespace2", nameof(Name));
}

[Include<IncludesTestModel2>]
public partial class IncludesTestModel3 : IModelDefinition
{
    public static readonly StringAttribute Name = new("MyNamespace3", nameof(Name));
}
