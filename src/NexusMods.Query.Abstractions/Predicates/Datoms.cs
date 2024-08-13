using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.Query.Abstractions.Engines.Abstract;
using NexusMods.Query.Abstractions.Engines.Slots;
using NexusMods.Query.Abstractions.Engines.Steps;
using Environment = NexusMods.Query.Abstractions.Engines.Environment;

namespace NexusMods.Query.Abstractions.Predicates;

public class Datoms<THighLevel, TLowLevel> : APredicate<IDb, EntityId, Attribute<THighLevel, TLowLevel>, THighLevel> 
    where THighLevel : notnull
{
    protected override Environment.Execute EmitCVCO<TSlotA, TSlotB, TSlotC, TSlotD>(TSlotA a, TSlotB b, TSlotC c, TSlotD d, Environment.Execute innerExpr)
    {
        return (ref Environment env) =>
        {
            var db = a.Get(ref env);
            var attr = c.Get(ref env);
            var attrId = attr.GetDbId(db.Registry.Id);
            foreach (var datom in db.Datoms(b.Get(ref env)))
            {
                if (datom.A != attrId)
                    continue;
                d.Set(ref env, ((Attribute<THighLevel, TLowLevel>.ReadDatom)datom.Resolved).V);
                innerExpr(ref env);
            }
        };
    }

    protected override Environment.Execute EmitCOCC<TSlotA, TSlotB, TSlotC, TSlotD>(TSlotA a, TSlotB b, TSlotC c, TSlotD d, Environment.Execute inner)
    {
        return (ref Environment env) =>
        {
            foreach (var datom in a.Get(ref env).Datoms(c.Get(ref env), d.Get(ref env)))
            {
                b.Set(ref env, datom.E);
                inner(ref env);
            }
        };
    }

    protected override Environment.Execute EmitCCCC<TSlotA, TSlotB, TSlotC, TSlotD>(TSlotA a, TSlotB b, TSlotC c, TSlotD d, Environment.Execute innerExpression)
    {
        return (ref Environment env) =>
        {
            var db = a.Get(ref env);
            var attr = c.Get(ref env);
            var attrId = attr.GetDbId(db.Registry.Id);
            var value = d.Get(ref env);
            foreach (var datom in db.Datoms(b.Get(ref env)))
            {
                if (datom.A != attrId)
                    continue;
                
                if (((Attribute<THighLevel, TLowLevel>.ReadDatom)datom.Resolved).V.Equals(value))
                    innerExpression(ref env);
            }
        };
    }

    private class StepCVCO(ISlot<IDb> a, ISlot<EntityId> b, ISlot<Attribute<THighLevel, TLowLevel>> c, ISlot<THighLevel> d, IStep innerExpression) : IStep
    {
        public void Execute(ref Environment environment)
        { 
            var db = a.Get(ref environment);
            var attr = c.Get(ref environment);
            var value = d.Get(ref environment);
            
            foreach (var datom in db.Datoms(attr, value))
            {
                b.Set(ref environment, datom.E);
                innerExpression.Execute(ref environment);
            }
        }
    }
    


    /// <summary>
    /// Constructs a foreach loop via Linq Expressions
    /// </summary>
    private Expression Foreach(Expression collExpr, Func<Expression, Expression> body)
    {
        var enumeratorCall = Expression.Call(collExpr, "GetEnumerator", null);
        var enumerableParam = Expression.Parameter(enumeratorCall.Type, "enumerator");
        var enumAssign = Expression.Assign(enumerableParam, enumeratorCall);
        
        var breakLabel = Expression.Label("LoopBreak");

        var loop = Expression.Loop(
            Expression.Block(
                new[] { enumerableParam },
                enumAssign,
                Expression.IfThenElse(
                    Expression.Call(enumerableParam, "MoveNext", null),
                    body(Expression.Property(enumerableParam, "Current")),
                    Expression.Break(breakLabel)
                ),
                Expression.Label(breakLabel)
            )
        );
        return loop;
    }
}
