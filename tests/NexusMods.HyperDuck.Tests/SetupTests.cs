namespace NexusMods.HyperDuck.Tests;

public class SetupTests
{
    /*
    [Test]
    public async Task OpenAndClose()
    {
        using var db = Database.Open(":memory:");
    }
    
    [Test]
    public async Task OpenAndCloseInMemory()
    {
        using var db = Database.Open(":memory:");
        using var con = db.Connect();
    }

    [Test]
    public async Task CanExecuteQuery()
    {
        using var db = Database.Open(":memory:");
        using var con = db.Connect();
        using var result = con.Query("SELECT 1 AS one");

        await Assert.That(result.ColumnCount).IsEqualTo((ulong)1);
    }

    [Test]
    public async Task CanGetColumnInfo()
    {
        using var db = Database.Open(":memory:");
        using var con = db.Connect();
        using var result = con.Query("SELECT 1 AS one, 'test' AS two");

        await Assert.That(result.ColumnCount).IsEqualTo((ulong)2);
        await Assert.That(result.GetColumnInfo(0).Name).IsEqualTo("one");
        await Assert.That(result.GetColumnInfo(1).Name).IsEqualTo("two");

        await Assert.That(result.GetColumnInfo(0).Type).IsEqualTo(DuckDbType.Integer);
        await Assert.That(result.GetColumnInfo(1).Type).IsEqualTo(DuckDbType.Varchar);
        
        await Assert.That(result.Columns.Count()).IsEqualTo(2);
        var cols = result.Columns.ToArray();
        await Assert.That(cols[0].Name).IsEqualTo("one");
        await Assert.That(cols[1].Name).IsEqualTo("two");
    }
    */
}