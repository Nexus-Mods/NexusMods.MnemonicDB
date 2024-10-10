using System;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds a timestamp
/// </summary>
public class TimestampAttribute(string ns, string name) : ScalarAttribute<DateTime, long>(ValueTag.Int64, ns, name)
{
    /// <inheritdoc />
    protected override long ToLowLevel(DateTime value) => value.ToFileTimeUtc();

    /// <inheritdoc />
    protected override DateTime FromLowLevel(long value, AttributeResolver resolver) => DateTime.FromFileTimeUtc(value);
}
