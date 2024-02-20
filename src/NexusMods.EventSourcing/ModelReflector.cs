using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;

namespace NexusMods.EventSourcing;

/// <summary>
/// Reflects over models and creates reader/writer functions for them.
/// </summary>
internal class ModelReflector<TTransaction>(IDatomStore store)
    where TTransaction : ITransaction
{
    private readonly ConcurrentDictionary<Type,object> _emitters = new();
    private readonly ConcurrentDictionary<Type, object> _readers = new();

    private delegate void EmitterFn<in TReadModel>(TTransaction tx, TReadModel model)
        where TReadModel : IReadModel;

    internal delegate TReadModel ReaderFn<out TReadModel>(EntityId id, IEntityIterator iterator, IDb db)
        where TReadModel : IReadModel;

    public void Add(TTransaction tx, IReadModel model)
    {
        EmitterFn<IReadModel> emitterFn;
        var modelType = model.GetType();
        if (!_emitters.TryGetValue(model.GetType(), out var found))
        {
            emitterFn = CreateEmitter(modelType);
            _emitters.TryAdd(modelType, emitterFn);
        }
        else
        {
            emitterFn = (EmitterFn<IReadModel>)found;
        }
        emitterFn(tx, model);
    }

    /// <summary>
    /// Reflects over
    /// </summary>
    /// <typeparam name="TReadModel"></typeparam>
    /// <returns></returns>
    private EmitterFn<IReadModel> CreateEmitter(Type readModel)
    {
        var properties = GetModelProperties(readModel);

        var entityParameter = Expression.Parameter(typeof(IReadModel), "entity");
        var txParameter = Expression.Parameter(typeof(TTransaction), "tx");

        var exprs = new List<Expression>();
        var idVariable = Expression.Variable(typeof(EntityId), "entityId");
        var castedVariable = Expression.Variable(readModel, "castedEntity");

        exprs.Add(Expression.Assign(castedVariable, Expression.Convert(entityParameter, readModel)));
        exprs.Add(Expression.Assign(idVariable, Expression.Property(entityParameter, "Id")));

        exprs.AddRange(from property in properties
            let method = property.Attribute.GetMethod("Add", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)!
            let value = Expression.Property(castedVariable, property.Property)
            select Expression.Call(null, method, txParameter, idVariable, value));

        var blockExpr = Expression.Block(new[] { idVariable, castedVariable}, exprs);

        var lambda = Expression.Lambda<EmitterFn<IReadModel>>(blockExpr, txParameter, entityParameter);
        return lambda.Compile();
    }

    private static IEnumerable<(Type Attribute, PropertyInfo Property)> GetModelProperties(Type readModel)
    {
        var properties = readModel
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
            .Where(p => p.GetCustomAttributes(typeof(FromAttribute<>), true).Any())
            .Select(p => (
                (p.GetCustomAttributes(typeof(FromAttribute<>), true).First() as IFromAttribute)!.AttributeType,
                p))
            .ToList();
        return properties;
    }

    public ReaderFn<TModel> GetReader<TModel>() where TModel : IReadModel
    {
        var modelType = typeof(TModel);
        if (_readers.TryGetValue(modelType, out var found))
            return (ReaderFn<TModel>)found;

        var readerFn = MakeReader<TModel>();
        _readers.TryAdd(modelType, readerFn);
        return readerFn;
    }

    public ReaderFn<TModel> MakeReader<TModel>() where TModel : IReadModel
    {
        var properties = GetModelProperties(typeof(TModel));

        var exprs = new List<Expression>();

        var whileTopLabel = Expression.Label("whileTop");
        var exitLabel = Expression.Label("exit");




        var entityIdParameter = Expression.Parameter(typeof(EntityId), "entityId");
        var iteratorParameter = Expression.Parameter(typeof(IEntityIterator), "iterator");
        var dbParameter = Expression.Parameter(typeof(IDb), "db");

        var newModelExpr = Expression.Variable(typeof(TModel), "newModel");

        var spanExpr = Expression.Property(iteratorParameter, "ValueSpan");
        var ctor = typeof(TModel).GetConstructor([typeof(ITransaction)])!;


        exprs.Add(Expression.Assign(newModelExpr, Expression.New(ctor, Expression.Constant(null, typeof(ITransaction)))));
        exprs.Add(Expression.Assign(Expression.Property(newModelExpr, "Id"), entityIdParameter));
        exprs.Add(Expression.Assign(Expression.Property(newModelExpr, "Db"), dbParameter));

        exprs.Add(Expression.Label(whileTopLabel));
        exprs.Add(Expression.IfThen(
            Expression.Not(Expression.Call(iteratorParameter, typeof(IEntityIterator).GetMethod("Next")!)),
            Expression.Break(exitLabel)));

        var cases = new List<SwitchCase>();

        foreach (var (attribute, property) in properties)
        {
            var readSpanExpr = store.GetValueReadExpression(attribute, spanExpr, out var attributeId);

            var assigned = Expression.Assign(Expression.Property(newModelExpr, property), readSpanExpr);

            cases.Add(Expression.SwitchCase(Expression.Block([assigned, Expression.Goto(whileTopLabel)]),
                Expression.Constant(attributeId)));
        }

        exprs.Add(Expression.Switch(Expression.Property(iteratorParameter, "AttributeId"), cases.ToArray()));

        exprs.Add(Expression.Goto(whileTopLabel));
        exprs.Add(Expression.Label(exitLabel));
        exprs.Add(newModelExpr);

        var block = Expression.Block(new[] {newModelExpr}, exprs);

        var lambda = Expression.Lambda<ReaderFn<TModel>>(block, entityIdParameter, iteratorParameter, dbParameter);
        return lambda.Compile();
    }
}
