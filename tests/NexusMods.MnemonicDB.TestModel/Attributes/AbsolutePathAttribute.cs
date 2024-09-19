using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public class AbsolutePathAttribute(string ns, string name) : ScalarAttribute<AbsolutePath, string>(ValueTags.Utf8, ns, name)
{ 
    protected override string ToLowLevel(AbsolutePath value)
    {
        return value.ToString();
    }
    protected override AbsolutePath FromLowLevel(string value, ValueTags tag, AttributeResolver resolver)
    {
        return resolver.ServiceProvider.GetRequiredService<IFileSystem>().FromUnsanitizedFullPath(value);
    }
}
