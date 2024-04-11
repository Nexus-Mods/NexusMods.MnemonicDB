using System;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Attributes;

public static class Transaction
{
    public static readonly Attribute<DateTime> CreatedAt = new("NexusMods.MnemonicDB.Transaction/CreatedAt");
}
