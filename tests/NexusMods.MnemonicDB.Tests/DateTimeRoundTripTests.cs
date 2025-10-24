using System;
using System.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.Traits;
using Transaction = NexusMods.MnemonicDB.Abstractions.BuiltInEntities.Transaction;

namespace NexusMods.MnemonicDB.Tests;

[WithServiceProvider]
public class DateTimeRoundTripTests : AMnemonicDBTest
{
    public DateTimeRoundTripTests(IServiceProvider provider) : base(provider)
    {
    }

    [Test]
    public async Task TransactionTimestamp_RoundTrips_ThroughDuckDbModelFunction()
    {
        // Create some data which will generate at least one transaction with a Timestamp
        await InsertExampleData();

        // Query via DuckDB model table function
        var rows = Connection.Query<(EntityId Id, DateTimeOffset Timestamp)>(
            "SELECT Id, Timestamp FROM mdb_Transaction()")
            .ToArray();

        // Ground truth: read raw datoms and resolve to DateTimeOffset
        var txDatoms = Connection.Db
            .Datoms(Transaction.Timestamp)
            .Resolved(Connection)
            .ToDictionary(d => d.E, d => (DateTimeOffset)d.V);

        // Compare per Id for exact equality
        foreach (var (id, ts) in rows)
        {
            if (txDatoms.TryGetValue(id, out var expected))
            {
                await Assert.That(ts).IsEqualTo(expected);
            }
        }
    }
}

