using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Tests;

public class DuckDBDatomStoreTests
{
    private readonly DuckDBDatomStore _db;
    private readonly ILogger<DuckDBDatomStoreTests> _logger;

    public DuckDBDatomStoreTests(ILogger<DuckDBDatomStoreTests> logger, IEnumerable<IDatomStore> stores)
    {
        _logger = logger;
        _db = stores.OfType<DuckDBDatomStore>().First();
    }

    [Fact]
    public void CanStart()
    {
        // Won't even run if we can't start the DB
        true.Should().BeTrue();
    }

    [Fact]
    public void CanInsertDatoms()
    {
        _db.Transact(
            (0x0042, 0x011, 3, 0x1),
            (0x0042, 0x012, "test", 0x1),
            (0x0042, 0x013, 0.4f, 0x1));

        var accumulator = new DatomAccumulator();
        _db.AllDatomsWithTx(in accumulator);
        var datoms = accumulator.Datoms;
        datoms.Should().HaveCount(3);
        datoms.Should().Contain((0x0042, 0x011, 3L, 0x1));
        datoms.Should().Contain((0x0042, 0x012, "test", 0x1));
        datoms.Should().Contain((0x0042, 0x013, 0.4f, 0x1));
    }

    [Fact]
    public void CanInsertALotOfDatoms()
    {
        var datoms = new List<(ulong e, ulong a, object v, ulong tx)>();
        for (var i = 0; i < 1024 * 3; i++)
        {
            var e = (ulong)i + 10000;
            datoms.Add((e, 0x01, i, e));
            datoms.Add((e, 0x02, (long)i, e));
            datoms.Add((e, 0x03, $"Datom: {i}", e));
            datoms.Add((e, 0x04, (i % 2) == 0, e));
            datoms.Add((e, 0x05, (double)i, e));
            datoms.Add((e, 0x06, (float)i, e));
            var arr = BitConverter.GetBytes(i);
            datoms.Add((e, 0x07, arr, e));
        }

        var sw = Stopwatch.StartNew();
        _db.Transact(datoms.ToArray());
        _logger.LogInformation("Inserted {Total} datoms in {Elapsed}", datoms.Count, sw.Elapsed);

        var accumulator = new DatomAccumulator();
        sw.Restart();
        _db.AllDatomsWithTx(in accumulator);
        _logger.LogInformation("Read {Total} datoms in {Elapsed}", accumulator.Datoms.Count, sw.Elapsed);

    }
}
