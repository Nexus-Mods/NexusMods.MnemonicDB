using System;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds a timestamp
/// </summary>
public class TimestampAttribute(string ns, string name) : ScalarAttribute<DateTimeOffset, long>(ValueTag.Int64, ns, name)
{
    /// <inheritdoc />
    protected override long ToLowLevel(DateTimeOffset value) => value.ToUnixTimeMilliseconds();

    /// <inheritdoc />
    protected override DateTimeOffset FromLowLevel(long value, AttributeResolver resolver) => DateTimeOffset.FromUnixTimeMilliseconds(value);
}
