using System;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.Exceptions;

public class UnsupportedLowLevelReadType(ValueTags tag) : Exception($"Unsupported low-level read type: {tag}")
{

}
