using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using NexusMods.Paths;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds an <see cref="RelativePath"/>.
/// </summary>
[PublicAPI]
public sealed class RelativePathAttribute(string ns, string name) : ScalarAttribute<RelativePath, string, Utf8InsensitiveSerializer>(ns, name)
{
    /// <inheritdoc/>
    protected override string ToLowLevel(RelativePath value) => value.ToString();

    /// <inheritdoc/>
    protected override RelativePath FromLowLevel(string value, AttributeResolver resolver)
    {
        return new RelativePath(value);
    }
}
