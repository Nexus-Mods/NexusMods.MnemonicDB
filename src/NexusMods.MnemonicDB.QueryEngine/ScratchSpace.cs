using System.Collections.Generic;
using System.Linq;

namespace NexusMods.MnemonicDB.QueryEngine;

public class ScratchSpace
{
    public void DoIt(IEnumerable<(int e, string a, string b)> db, string[] names)
    {

        var setA = Enumerable.Range(0, 10);
        var setB = Enumerable.Range(5, 15);

        var results = Query.New()
            .From(setA, out var a)
            .From(setB, out var b)
            .Eq(a, b)
            .Run(a);
    }
}
