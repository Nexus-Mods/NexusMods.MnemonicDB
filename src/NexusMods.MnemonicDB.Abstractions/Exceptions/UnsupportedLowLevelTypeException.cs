using System;

namespace NexusMods.MnemonicDB.Abstractions.Exceptions;

/// <summary>
/// Thrown when the low-level type of some attribute is not something the database can handle.
/// </summary>
public class UnsupportedLowLevelWriteType<TValue>(TValue value) :
    Exception($"Unsupported low-level write type: {value?.GetType().ToString() ?? "null"} for value: {value}")
{

}
