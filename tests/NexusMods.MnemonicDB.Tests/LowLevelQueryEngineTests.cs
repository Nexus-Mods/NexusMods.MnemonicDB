using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.QueryV2;
using R3;

namespace NexusMods.MnemonicDB.Tests;

public class LowLevelQueryEngineTests(IServiceProvider provider) : AMnemonicDBTest(provider)
{

    [Fact]
    public void CanGetSingleColumnResult()
    {
        using var result = Query.Query<int>("SELECT 1 AS value");
        result.ToList().Should().BeEquivalentTo([1]);
    }
    
    [Fact]
    public void CanGetMultipleColumnsResult()
    {
        using var result = Query.Query<(int, int)>("SELECT 1 AS value1, 42 AS value2");
        result.ToList().Should().BeEquivalentTo([(1, 42)]);
    }

    [Fact]
    public void CanReturnStringResults()
    {
        using var result = Query.Query<(string, string)>("SELECT 'foo' AS value, 'this is a test of a longer string that will not be inlined' AS long_value");
        result.ToList().Should().BeEquivalentTo([("foo", "this is a test of a longer string that will not be inlined")]);
    }

    [Fact]
    public void CanReturnHashValue()
    {
        using var result = Query.Query<Hash>("SELECT 42::UBIGINT AS value");
        result.ToList().Should().BeEquivalentTo([Hash.From(42)]);
    }

    [Fact]
    public void CanQueryTableFunction()
    {
        Query.Register(new RangeTableFunction("range10"));
        
        using var result = Query.Query<int>("SELECT value FROM range10()");
        result.ToList().Should().BeEquivalentTo([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);
    }
    
    class RangeTableFunction : TableFunction
    {
        public RangeTableFunction(string name) : base(name, [])
        {
        }

        protected override void Write(DuckDBChunkWriter writer, object? state, object? initData)
        {
            if (state is not FnState fnState)
                throw new InvalidOperationException("Invalid state for RangeTableFunction.");

            if (fnState.Current >= 10)
            {
                writer.Length = 0;
                return;
            }
            
            var column = writer.GetVector<int>(0);
            if (column.IsEmpty)
                return;
            
            for (var i = 0; i < 10; i++)
            {
                column[i] = i;
            }
            writer.Length = 10;
            fnState.Current += 10;
        }

        internal record FnState
        {
            public int Current { get; set; }
        }

        protected override void Bind(ref BindInfoWriter info)
        {
            info.AddColumn<int>("value");
            info.SetBindData(new FnState{ Current = 0 });
        }
    }

    [Fact]
    public async Task CanQueryModsByTable()
    {
        await InsertExampleData();
        
        using var result = Query.Query<(EntityId, string)>("SELECT Id, Name FROM mdb_Mod()");
        
        result.ToList().Should().BeEquivalentTo(
            [
                (PartitionId.Entity.MakeEntityId(2), "Mod1 - Updated"),
                (PartitionId.Entity.MakeEntityId(6), "Mod2 - Updated"),
                (PartitionId.Entity.MakeEntityId(0xA), "Mod3 - Updated"),
            ]);
        
    }
}
