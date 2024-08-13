using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Query.Abstractions.Engines;
using NexusMods.Query.Abstractions.Engines.Abstract;
using NexusMods.Query.Abstractions.Engines.Slots;
using Environment = NexusMods.Query.Abstractions.Engines.Environment;

namespace NexusMods.Query.Abstractions.Predicates;

public class LazyBuilder
{
    public LazyBuilder()
    {
        
    }

    public Func<IDb, TIn, List<TOut>> Build<TIn, TOut>(RootQuery query) 
        where TOut : notnull 
        where TIn : notnull
    {
        /*
        Dictionary<IVariable, int> variables = new();
        
        foreach (var variable in query.Inputs)
        {
            variables.TryAdd(variable, variables.Count);
        }
        
        foreach (var variable in query.InnerVariables)
        {
            variables.TryAdd(variable, variables.Count);
        }
        variables.TryAdd(query.Output, variables.Count);
        
        var accILVar = Variable<List<TOut>>.New("acc");
        variables.TryAdd(accILVar, variables.Count);
        
        var outSlot = new ObjectSlot<TOut>(variables[query.Output]);
        var accSlot = new ObjectSlot<List<TOut>>(variables[accILVar]);
        var envSize = variables.Count;
        var dbSlot = new ObjectSlot<IDb>(variables[ConstantNodes.Db]);
        var argASlot = new ObjectSlot<TIn>(variables[query.Inputs[0]]);


        var chain = query.Goal.Emit(variables, LastInChain);
        
        return (db, input) =>
        {
            var env = new Environment(envSize);
            accSlot.Set(ref env, []);
            dbSlot.Set(ref env, db);
            argASlot.Set(ref env, input);
            chain(ref env);
            return accSlot.Get(ref env);
        };

        void LastInChain(ref Environment env)
        {
            var acc = accSlot.Get(ref env);
            acc.Add(outSlot.Get(ref env));
        }
        */
        throw new NotSupportedException();
    }
    
    
    public Func<IDb, List<TOut>> Build<TOut>(RootQuery query) 
        where TOut : notnull 
    {
        var definition = new EnvironmentDefinition(query);
        var accILVar = Variable<List<TOut>>.New("acc");
        definition.Add(accILVar);
        
        var dbSlot = definition.GetSlot(ConstantNodes.Db);
        
        var accSlot = definition.GetSlot(accILVar);
        var outSlot = definition.GetSlot((Variable<TOut>)query.Output);
        
        var lastInChain = (Environment.Execute)GetType().GetMethod(nameof(LastInChain), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(accSlot.GetType(), outSlot.GetType(), typeof(TOut))
            .Invoke(null, [accSlot, outSlot])!;
        
        var chain = query.Goal.Emit(definition, lastInChain);
        
        var valueSize = definition.ValueSpanSize;
        var objectSize = definition.ObjectSpanSize;
        
        return db =>
        {
            Span<byte> values = stackalloc byte[valueSize];
            var objects = new object[objectSize];
            var env = new Environment(objects, values);
            
            dbSlot.Set(ref env, db);
            accSlot.Set(ref env, []);
            chain(ref env);
            return accSlot.Get(ref env);
        };
    }
    
    [UsedImplicitly]
    private static Environment.Execute LastInChain<TAccSlot, TResultSlot, TOut>(TAccSlot accSlot, TResultSlot outSlot)
        where TResultSlot : ISlot<TOut>
        where TAccSlot : ISlot<List<TOut>>
    {
        return (ref Environment env) =>
        {
            var acc = accSlot.Get(ref env);
            acc.Add(outSlot.Get(ref env));
        };
    }
}
