using System.Runtime.InteropServices;
using Assert = TUnit.Assertions.Assert;

namespace NexusMods.HyperDuck.Tests;

public class QueryTests
{
    [Test]
    public async Task CanGetResultChunks()
    {
        using var db = Database.OpenInMemory();
        using var con = db.Connect();
        using var cmd = con.Query("SELECT 1 AS a, 2 AS b");

        using var chunk = cmd.FetchChunk();
        await Assert.That(chunk.IsValid).IsTrue();
        await Assert.That(chunk.Size).IsEqualTo((ulong)1);
        await Assert.That(chunk.ColumnCount).IsEqualTo((ulong)2);
    }
 
    [Test]
    public async Task CanGetIntegerResults()
    {
        using var db = Database.OpenInMemory();
        using var con = db.Connect();
        using var result = con.Query("SELECT * FROM generate_series(1, 10, 1);");
        
        await Assert.That(result.GetColumnInfo(0).Type).IsEqualTo(DuckDbType.BigInt);
        
        using var chunk = result.FetchChunk();
        await Assert.That(chunk.IsValid).IsTrue();
        await Assert.That(chunk.Size).IsEqualTo((ulong)10);

        var col = chunk[0];
        var vec = MemoryMarshal.Cast<byte, long>(col.GetData()).ToArray();
        
        await Assert.That(vec.Length).IsEqualTo(10);
        await Assert.That(vec.Take(10)).IsEquivalentTo(Enumerable.Range(1, 10).Select(s => (long)s));

        var data = col.GetData<long>()[..(int)chunk.Size].ToArray();
        await Assert.That(data.Length).IsEqualTo(10);
        await Assert.That(data).IsEquivalentTo(Enumerable.Range(1, 10).Select(s => (long)s));
    }
    
    [Test]
    public async Task GetGetStringResults()
    {
        using var db = Database.OpenInMemory();
        using var con = db.Connect();
        using var result = con.Query("SELECT 'Hello' AS a, 'A really long string that cannot be inlined' AS b");

        await Assert.That(result.GetColumnInfo(0).Type).IsEqualTo(DuckDbType.Varchar);
        await Assert.That(result.GetColumnInfo(1).Type).IsEqualTo(DuckDbType.Varchar);

        using var chunk = result.FetchChunk();
        await Assert.That(chunk.IsValid).IsTrue();
        await Assert.That(chunk.Size).IsEqualTo((ulong)1);

        var dataA = chunk[0].GetData<StringElement>().ToArray();
        var dataB = chunk[1].GetData<StringElement>().ToArray();
        

        await Assert.That(dataA.Length).IsEqualTo(1);
        await Assert.That(dataA[0].GetString()).IsEqualTo("Hello");

        await Assert.That(dataB.Length).IsEqualTo(1);
        await Assert.That(dataB[0].GetString()).IsEqualTo("A really long string that cannot be inlined");
        
    }

    [Test]
    public async Task CanReadListTypes()
    {
        using var db = Database.OpenInMemory();
        using var con = db.Connect();
        using var result = con.Query("SELECT [1, 2, 3] as lst");
        
        using var colType = result.GetColumnInfo(0).GetLogicalType();
        await Assert.That(result.GetColumnInfo(0).Type).IsEqualTo(DuckDbType.List);

        using var subType = colType.ListChildType();
        await Assert.That(subType.TypeId).IsEqualTo(DuckDbType.Integer);
        
        using var chunk = result.FetchChunk();
        await Assert.That(chunk.IsValid).IsTrue();
        await Assert.That(chunk.Size).IsEqualTo((ulong)1);

        {
            var data = chunk[0].GetData<ListEntry>();
            await Assert.That(data[0].Length).IsEqualTo((ulong)3);
        }

        {
            var data = chunk[0].GetData<ListEntry>();
            var subData = chunk[0].GetListChild().GetData<uint>();
            var sliced = subData.Slice((int)data[0].Offset, (int)(data[0].Offset + data[0].Length));
            await Assert.That(sliced.ToArray()).IsEquivalentTo(new uint[] { 1, 2, 3 });

        }
    }



}
