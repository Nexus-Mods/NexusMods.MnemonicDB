using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Tests;

public class DuckDBDatomStoreTests
{
    private readonly DuckDBDatomStore _db;
    private readonly ILogger<DuckDBDatomStoreTests> _logger;

    public DuckDBDatomStoreTests(ILogger<DuckDBDatomStoreTests> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _db = new DuckDBDatomStore(serviceProvider.GetRequiredService<ILogger<DuckDBDatomStore>>());
    }

    [Fact]
    public void CanStart()
    {
        // Won't even run if we can't start the DB
        true.Should().BeTrue();
    }

    [Fact]
    public void CanGetinitialAttributes()
    {
        var attrs = _db.GetDbAttributes();
        attrs.Should().HaveCountGreaterOrEqualTo(3);

        var firstAttr = attrs.First();
        firstAttr.Name.Should().Be("$type");
        firstAttr.ValueType.Should().Be(ValueTypes.UHugeInt);

    }

    [Fact]
    public void CanInsertDatoms()
    {
        var entityId = Ids.MinId(IdSpace.Entity);
        var socket = new ArrayDatomSinkSocket([
            (entityId, 0x011, 3, 0x1),
            (entityId, 0x012, "test", 0x1),
            (entityId, 0x013, 0.4f, 0x1)
        ]);
        _db.Transact(ref socket);

        var accumulator = new DatomAccumulator(true);
        _db.AllDatomsWithTx(in accumulator);
        var datoms = accumulator.Datoms;
        datoms.Should().HaveCount(3);
        datoms.Should().Contain((entityId, 0x011, 3L, 0x1));
        datoms.Should().Contain((entityId, 0x012, "test", 0x1));
        datoms.Should().Contain((entityId, 0x013, 0.4f, 0x1));
    }

    [Fact]
    public void CanInsertALotOfDatoms()
    {
        var datoms = new List<(ulong e, ulong a, object v, ulong tx)>();
        for (var i = 0; i < 1024 * 16; i++)
        {
            var e = Ids.MinId(IdSpace.Entity) + (ulong)i;
            datoms.Add((e, 0x01, (ulong)i, e));
            datoms.Add((e, 0x02, (long)i, e));
            datoms.Add((e, 0x03, $"Datom: {i}", e));
            datoms.Add((e, 0x04, UInt128.MaxValue/2, e));
            datoms.Add((e, 0x05, (double)i, e));
            datoms.Add((e, 0x06, (float)i, e));
            var arr = BitConverter.GetBytes(i);
            datoms.Add((e, 0x07, arr, e));
        }

        var socket = new ArrayDatomSinkSocket(datoms.ToArray());
        var sw = Stopwatch.StartNew();
        _db.Transact(ref socket);
        _logger.LogInformation("Inserted {Total} datoms in {Elapsed}", datoms.Count, sw.Elapsed);

        var accumulator = new DatomAccumulator(true);
        sw.Restart();
        _db.AllDatomsWithTx(in accumulator);
        _logger.LogInformation("Read {Total} datoms in {Elapsed}", accumulator.Datoms.Count, sw.Elapsed);

        datoms.Count.Should().Be(accumulator.Datoms.Count);

        for (var i = 0; i < datoms.Count; i++)
        {
            accumulator.Datoms[i].Should().BeEquivalentTo(datoms[i], $"Datom {i} should match");
        }

    }
}
