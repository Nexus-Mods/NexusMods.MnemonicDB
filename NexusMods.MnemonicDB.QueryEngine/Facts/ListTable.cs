using System;
using System.Collections;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.QueryEngine.Facts;

public class ListTable<TFact> : ITable<TFact>
    where TFact : IFact
{
    private readonly List<TFact> _facts;
    public LVar[] Columns { get; }
    public Type FactType { get; }

    public ListTable(LVar[] columns, List<TFact> facts)
    {
        Columns = columns;
        FactType = typeof(TFact);
        _facts = facts;
    }

    public IEnumerator<TFact> GetEnumerator()
    {
        return _facts.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
