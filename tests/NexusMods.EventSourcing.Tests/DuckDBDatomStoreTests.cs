using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.Tests;

public class DuckDBDatomStoreTests
{
    private readonly DuckDBDatomStore _db;

    public DuckDBDatomStoreTests(IEnumerable<IDatomStore> stores)
    {
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
        _db.AllDatomsWithTx(accumulator);
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
        for (var i = 0; i < 10000; i++)
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

        _db.Transact(datoms.ToArray());


    }
}
