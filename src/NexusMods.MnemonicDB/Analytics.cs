using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.MnemonicDB;

internal class Analytics : IAnalytics
{
    private readonly IDb _db;
    private readonly Lazy<FrozenSet<EntityId>> _latestTxIds;

    internal Analytics(IDb db)
    {
        _db = db;
        _latestTxIds = new Lazy<FrozenSet<EntityId>>(CalculateLatestTxIds);
    }

    private FrozenSet<EntityId> CalculateLatestTxIds()
    {
        var tx = _db.BasisTxId;
        var latestDatoms = _db.Datoms(tx);

        var ids = new HashSet<EntityId>();
        foreach (var datom in latestDatoms)
        {
            ids.Add(datom.E);
            if (datom is ReferenceAttribute.ReadDatom referenceDatom)
            {
                ids.Add(referenceDatom.V);
            }
            else if (datom is ReferencesAttribute.ReadDatom referencesDatom)
            {
                ids.Add(referencesDatom.V);
            }
        }
        return ids.ToFrozenSet();
    }


    public FrozenSet<EntityId> LatestTxIds => _latestTxIds.Value;
}
