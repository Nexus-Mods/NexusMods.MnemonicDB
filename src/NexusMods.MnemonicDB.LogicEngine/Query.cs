using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.MnemonicDB.LogicEngine;

public static class Query
{
    public static PartialQuery<TArg1> New<TArg1>(out LVar<TArg1> arg1)
    {
        throw new NotImplementedException();
    }
    
}

public class PartialQuery<TArg1>
{
    public PartialQuery<TArg1> Where(params Predicate[] predicates)
    {
        return this;
    }
    
    public IQuery<TArg1, TRet> Return<TRet>(LVar<TRet> returnArg)
    {
        throw new NotImplementedException();
    }

    public PartialQuery<TArg1> Declare<T>(out LVar<T> v)
    {
        v = LVar.Create<T>();
        return this;
    }
}

public interface IQuery<TArg1, TRet>
{
    public ISet<TRet> Run(IDb db, TArg1 arg1);

    public ISet<TRet> Observe(IConnection connection, IObservable<TArg1> arg1);
}
