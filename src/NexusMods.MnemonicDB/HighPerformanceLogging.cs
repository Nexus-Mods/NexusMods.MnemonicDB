using System;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB;

internal static partial class HighPerformanceLogging
{
    public static readonly TimeSpan LoggingThreshold = TimeSpan.FromSeconds(1);

    [LoggerMessage(
        EventId = 0, EventName = nameof(TransactionPostProcessed),
        Level = LogLevel.Trace,
        Message = "Transaction `{id}` post-processed in {milliseconds}ms")]
    public static partial void TransactionPostProcessed(
        ILogger logger,
        TxId id,
        double milliseconds);

    [LoggerMessage(
        EventId = 1, EventName = nameof(TransactionPostProcessedLong),
        Level = LogLevel.Warning,
        Message = "Transaction `{id}` post-processed in {elapsed}, took longer than threshold")]
    public static partial void TransactionPostProcessedLong(
        ILogger logger,
        TxId id,
        TimeSpan elapsed);
}
