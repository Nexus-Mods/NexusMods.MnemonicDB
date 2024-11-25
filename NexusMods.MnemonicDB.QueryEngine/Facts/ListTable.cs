using System;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.QueryEngine.Facts;

public class ListTable<TFact> : ITable<TFact>
    where TFact : IFact
{
    public LVar[] Columns { get; }
    public Type FactType { get; }
    public IEnumerable<TFact> Facts { get; }

    public ListTable(LVar[] columns, List<TFact> facts)
    {
        Columns = columns;
        FactType = typeof(TFact);
        Facts = facts;
    }

}
