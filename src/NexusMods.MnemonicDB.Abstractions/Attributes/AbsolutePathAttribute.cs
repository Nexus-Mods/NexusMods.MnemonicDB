using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds an <see cref="AbsolutePath"/>.
/// </summary>
[PublicAPI]
public sealed class AbsolutePathAttribute(string ns, string name) : ScalarAttribute<AbsolutePath, string, Utf8InsensitiveSerializer>(ns, name)
{
    /// <inheritdoc/>
    protected override string ToLowLevel(AbsolutePath value) => value.ToString();

    /// <inheritdoc/>
    protected override AbsolutePath FromLowLevel(string value, AttributeResolver resolver)
    {
        return resolver.ServiceProvider.GetRequiredService<IFileSystem>().FromUnsanitizedFullPath(value);
    }
}
