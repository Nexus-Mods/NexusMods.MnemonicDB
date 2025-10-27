using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.SourceGenerator.Tests;

public partial class MyModelArrayTest : IModelDefinition
{
    public static readonly MyAttribute MyAttribute = new("MyNamespace", nameof(MyAttribute));
}

public sealed class MyAttribute(string ns, string name) : ScalarAttribute<int[], int, Int32Serializer>(ns, name)
{
    public override int ToLowLevel(int[] value)
    {
        throw new NotImplementedException();
    }

    public override int[] FromLowLevel(int value, AttributeResolver resolver)
    {
        throw new NotImplementedException();
    }
}
