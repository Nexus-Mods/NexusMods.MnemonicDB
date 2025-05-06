using System;
using NexusMods.Cascade;
using NexusMods.Cascade.Structures;
using NexusMods.MnemonicDB.Abstractions.Cascade.Flows;

namespace NexusMods.MnemonicDB.Abstractions.Cascade;

public static class Query
{
    public static readonly Inlet<IDb> Db = new();

    public static readonly Flow<(EntityId E, EntityId Tx)> LatestTxForEntity = new LatestTxForEntity(Db);

    internal static DbUpdate ToDbUpdate(ReadOnlySpan<Diff<IDb>> diffSet)
    {
        switch (diffSet.Length)
        {
            case 0:
                return new DbUpdate(null, null, UpdateType.None);
            case 1:
            {
                var (db, delta) = diffSet[0];
                if (delta < 1)
                {
                    return new DbUpdate(db, null, UpdateType.RemoveAndAdd);
                }
                else
                {
                    return new DbUpdate(null, db, UpdateType.Init);
                }
            }
            case 2:
            {
                var (db1, delta1) = diffSet[0];
                var (db2, delta2) = diffSet[1];

                // Swap the ordering so it's old -> new
                if (delta2 < delta1)
                {
                    (db1, delta1) = (db2, delta2);
                }

                if (db1.BasisTxId.Value + 1 == db2.BasisTxId.Value)
                    return new DbUpdate(db1, db2, UpdateType.NextTx);
                else
                    return new DbUpdate(db1, db2, UpdateType.RemoveAndAdd);
            }
            default:
                throw new InvalidOperationException($"Invalid number of databases {diffSet.Length} in diff set");
        }
    }
}
