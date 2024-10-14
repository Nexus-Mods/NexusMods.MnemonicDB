using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.MnemonicDB.Storage;

namespace NexusMods.MnemonicDB.InternalTxFunctions;

/// <summary>
/// A transaction that contains one or more user defined functions as well as an index segment
/// </summary>
internal sealed class CompoundTransaction : AInternalFn
{
    private readonly IndexSegment _segment;
    private readonly HashSet<ITxFunction> _userFunctions;

    /// <summary>
    /// A transaction that contains one or more user defined functions as well as an index segment
    /// </summary>
    public CompoundTransaction(IndexSegment segment, HashSet<ITxFunction> userFunctions)
    {
        _segment = segment;
        _userFunctions = userFunctions;
    }

    /// <summary>
    /// The connection to use for the transaction
    /// </summary>
    public required IConnection Connection { get; init; }

    /// <inheritdoc />
    public override void Execute(DatomStore store)
    {
        var secondaryBuilder = new IndexSegmentBuilder(store.AttributeCache);
        try
        {
            var db = new Db(store.CurrentSnapshot, store.AsOfTxId, store.AttributeCache);
            db.Connection = Connection;
            var tx = new InternalTransaction(db, secondaryBuilder);
            
            foreach (var fn in _userFunctions)
            {
                fn.Apply(tx, db);
            }
        }
        catch (Exception ex)
        {
            store.Logger.LogError(ex, "Failed to apply transaction functions");
            throw;
        }
        var secondaryData = secondaryBuilder.Build();
        var datoms = _segment.Concat(secondaryData);

        store.LogDatoms(datoms, enableStats: store.Logger.IsEnabled(LogLevel.Debug));
    }
}
