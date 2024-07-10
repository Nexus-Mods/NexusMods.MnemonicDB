using MnemonicDB.Query;
using MnemonicDB.Query.Engines.Datalog.Prolog;
using MnemonicDB.Query.Sources;

namespace NexusMods.MnemonicDB.Query.Tests;

public class LazyQueryTests
{
    private readonly Engine _engine;

    public LazyQueryTests()
    {
        _engine = new Engine([
            new Range<int, int>()
        ]);
    }

    [Fact]
    public async Task CanPerformSimpleQuery()
    {
        var x = Term.LVar<int>();
        var query = new IFact[]
        {
            new Fact<int, int>(Source.Range, 5, x)
        };
        
        var results = _engine.Query(query, x);

        await Verify(results);
    }

    [Fact]
    public async Task CanPerformSimpleQueryWithParamater()
    {
        var x = Term.LVar<int>();
        var max = Term.LVar<int>();
        
        var query = new IFact[]
        {
            new Fact<int, int>(Source.Range, max, x)
        };
        var results = _engine.Query(query, max, 20, x);
        
        await Verify(results);
    }
    
}
