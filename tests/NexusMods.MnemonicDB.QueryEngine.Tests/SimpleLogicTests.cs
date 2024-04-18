using FluentAssertions;

namespace NexusMods.MnemonicDB.QueryEngine.Tests;

public class SimpleLogicTests
{

    [Fact]
    public void CanUnifyTwoSequences()
    {

        var setA = Enumerable.Range(0, 10);
        var setB = Enumerable.Range(5, 15);

        // Not optimal as this is O(m*n) where m and n are the sizes of the sets
        // but it's an example of how the engine can be used to unify two unrelated
        // sets of data
        var results = Query.New()
            .From(setA, out var a)
            .From(setB, out var b)
            .Eq(a, b)
            .Run(a);

        results.Should().BeEquivalentTo([5, 6, 7, 8, 9]);
    }

    [Fact]
    public void CanFindOneSequenceInAnother()
    {

        var setA = Enumerable.Range(0, 10);
        var setB = Enumerable.Range(5, 15);

        // Here we're getting items from setA, and then checking if they're in setB
        // this is more optimized than the previous test which checks all combinations
        // and throws away the ones that don't match
        var results = Query.New()
            .From(setA, out var a)
            .From(setB, a)
            .Run(a);

        results.Should().BeEquivalentTo([5, 6, 7, 8, 9]);
    }

    [Fact]
    public void CanLookupValuesFromADictionary()
    {
        var a = new[] { 7, 0, 11, 6, 0, 17, 8 };

        // Makes a dictionary of the values and their indexes
        var b = "abcdefghijklmnopqrstuvwxyz"
            .Select((c, i) => (c, i))
            .ToDictionary(x => x.i, x => x.c);


        // Look up the characters in the dictionary for each value in a sequence
        var results = Query.New()
            .From(a, out var aVar)
            .From(b, aVar, out var charVar)
            .Run(charVar);

        results.Should().BeEquivalentTo("halgari");
    }
}
