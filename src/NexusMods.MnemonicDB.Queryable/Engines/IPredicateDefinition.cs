using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Queryable.AbstractSyntaxTree;

namespace NexusMods.MnemonicDB.Queryable.Engines;

public interface IPredicateDefinition
{
    public Symbol Name { get; }
    
    public int Arity { get; }

    public Func<ILVar[], bool> MakeStepper(Dictionary<ILVar, int> indexes);
}

public interface IPredicateDefinition<T1, T2> : IPredicateDefinition
{
    
}
