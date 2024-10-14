using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.TestModel.Attributes;

public sealed class AbsolutePathAttribute(string ns, string name) : ScalarAttribute<AbsolutePath, string>(ValueTag.Utf8, ns, name)
{ 
    protected override string ToLowLevel(AbsolutePath value) 
        => value.ToString();

    protected override AbsolutePath FromLowLevel(string value, AttributeResolver resolver) 
        => resolver.ServiceProvider.GetRequiredService<IFileSystem>().FromUnsanitizedFullPath(value);
}
