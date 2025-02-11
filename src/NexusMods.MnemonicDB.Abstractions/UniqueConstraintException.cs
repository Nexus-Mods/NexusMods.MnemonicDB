using System;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Thrown when a unique constraint on a attribute is
/// </summary>
public class UniqueConstraintException(Datom datom) : Exception($"Unique constraint violation on datom: {datom}");
