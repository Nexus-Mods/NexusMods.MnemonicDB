using System;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Query;

public record struct FactKey(Type FactType, Symbol Predicate);
