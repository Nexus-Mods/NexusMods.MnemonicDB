using System;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.Queryable.Engines;

public interface IPredicateDefinitionFactory : IPredicateDefinition
{
    public bool TrySpecialize<T1, T2>(out IPredicateDefinition<T1, T2> specialized);
}
