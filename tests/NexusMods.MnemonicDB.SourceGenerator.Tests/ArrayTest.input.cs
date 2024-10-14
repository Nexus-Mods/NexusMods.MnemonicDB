using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.SourceGenerator.Tests;

public partial class MyModelArrayTest : IModelDefinition
{
    public static readonly MyAttribute MyAttribute = new("MyNamespace", nameof(MyAttribute));
}

public class MyAttribute(string ns, string name) : ScalarAttribute<int[], int>(ValueTag.Blob, ns, name)
{
    protected override int ToLowLevel(int[] value)
    {
        throw new NotImplementedException();
    }

    protected override int[] FromLowLevel(int value, AttributeResolver resolver)
    {
        throw new NotImplementedException();
    }
}
