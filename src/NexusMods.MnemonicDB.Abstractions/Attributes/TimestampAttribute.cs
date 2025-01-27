using System;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds a timestamp
/// </summary>
[PublicAPI]
public sealed class TimestampAttribute(string ns, string name) : ScalarAttribute<DateTimeOffset, long, Int64Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override long ToLowLevel(DateTimeOffset value) => value.UtcTicks;

    /// <inheritdoc />
    protected override DateTimeOffset FromLowLevel(long value, AttributeResolver resolver) => new(value, TimeSpan.Zero);
}
