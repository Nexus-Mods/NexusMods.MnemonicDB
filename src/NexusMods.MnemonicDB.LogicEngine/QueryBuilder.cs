using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.LogicEngine.Sources;

namespace NexusMods.MnemonicDB.LogicEngine;

public static partial class QueryBuilder
{
    /// <summary>
    /// The global DB context for a query
    /// </summary>
    public static readonly LVar GlobalDb = LVar.Create("db");
    public static IPredicate Datoms<THighLevel>(LVar eLvar, IWritableAttribute<THighLevel> attr, THighLevel value)
        where THighLevel : notnull
    {
        return IPredicate.Create<Datoms>(GlobalDb, eLvar, attr, value);
    }
    
    public static IPredicate Datoms(LVar eLVar, IAttribute attr, LVar value)
    {
        return IPredicate.Create<Datoms>(GlobalDb, eLVar, attr, value);
    }
    
    public static IPredicate And(params IPredicate[] predicates)
    {
        return IPredicate.Create<And>(predicates);
    }
}
