using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.TestModel;

public partial class ParentA : IModelDefinition
{
    private const string Namespace = "NexusMods.MnemonicDB.ParentA";
    
    public static readonly StringAttribute Name = new(Namespace, "Name");
}

public partial class ParentB : IModelDefinition
{
    private const string Namespace = "NexusMods.MnemonicDB.ParentB";
    
    public static readonly StringAttribute Name = new(Namespace, "Name");
}

[Include<ParentA>]
[Include<ParentB>]
public partial class Child : IModelDefinition
{
    private const string Namespace = "NexusMods.MnemonicDB.Child";
    
    public static readonly StringAttribute Name = new(Namespace, "Name");
}
