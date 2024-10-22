using System;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds a timestamp
/// </summary>
public class TimestampAttribute(string ns, string name) : ScalarAttribute<DateTimeOffset, long, Int64Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override long ToLowLevel(DateTimeOffset value) => value.ToUnixTimeMilliseconds();

    /// <inheritdoc />
    protected override DateTimeOffset FromLowLevel(long value, AttributeResolver resolver) => DateTimeOffset.FromUnixTimeMilliseconds(value);
}
