using System;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.QueryEngine.Facts;

public interface ITable
{
    /// <summary>
    /// The LVars that are columns in the table
    /// </summary>
    public LVar[] Columns { get; }
    
    public Type FactType { get; }

    public ITable HashJoin(ITable other)
    {
        throw new NotImplementedException();

    }
}

public interface ITable<out TFact> : ITable, IEnumerable<TFact>
    where TFact : IFact
{
}

