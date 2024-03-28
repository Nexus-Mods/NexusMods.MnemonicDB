﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.Models;

namespace NexusMods.EventSourcing;

/// <summary>
///     Reflects over models and creates reader/writer functions for them.
/// </summary>
internal class ModelReflector<TTransaction>(IDatomStore store)
    where TTransaction : ITransaction
{
    private readonly ConcurrentDictionary<Type, object> _activeReaders = new();
    private readonly ConcurrentDictionary<Type, object> _constructors = new();
    private readonly ConcurrentDictionary<Type, object> _emitters = new();
    private readonly ConcurrentDictionary<Type, object> _readers = new();

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
    ///     Reflects over
    /// </summary>
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
            let method = property.Attribute.GetMethod("Add",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)!
            let value = Expression.Property(castedVariable, property.Property)
            select Expression.Call(null, method, txParameter, idVariable, value));

        var blockExpr = Expression.Block(new[] { idVariable, castedVariable }, exprs);

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


    private ReaderFn<TModel> MakeReader<TModel>() where TModel : IReadModel
    {
        var properties = GetModelProperties(typeof(TModel));

        var exprs = new List<Expression>();

        var whileTopLabel = Expression.Label("whileTop");
        var exitLabel = Expression.Label("exit");


        var entityIdParameter = Expression.Parameter(typeof(EntityId), "entityId");
        var iteratorParameter = Expression.Parameter(typeof(IEnumerator<IReadDatom>), "iterator");
        var dbParameter = Expression.Parameter(typeof(IDb), "db");

        var newModelExpr = Expression.Variable(typeof(TModel), "newModel");

        var ctor = typeof(TModel).GetConstructor([typeof(ITransaction)])!;


        exprs.Add(
            Expression.Assign(newModelExpr, Expression.New(ctor, Expression.Constant(null, typeof(ITransaction)))));
        exprs.Add(Expression.Assign(Expression.Property(newModelExpr, "Id"), entityIdParameter));
        exprs.Add(Expression.Assign(Expression.Property(newModelExpr, "Db"), dbParameter));

        exprs.Add(Expression.Label(whileTopLabel));
        exprs.Add(Expression.IfThen(
            Expression.Not(Expression.Call(iteratorParameter, typeof(IEnumerator).GetMethod("MoveNext")!)),
            Expression.Break(exitLabel)));


        foreach (var (attribute, property) in properties)
        {
            var readDatomType = store.GetReadDatomType(attribute);

            var ifExpr = Expression.IfThen(
                Expression.TypeIs(Expression.Property(iteratorParameter, "Current"), readDatomType),
                Expression.Assign(Expression.Property(newModelExpr, property),
                    Expression.Property(
                        Expression.Convert(Expression.Property(iteratorParameter, "Current"), readDatomType), "V")));

            exprs.Add(ifExpr);
        }

        exprs.Add(Expression.Goto(whileTopLabel));
        exprs.Add(Expression.Label(exitLabel));
        exprs.Add(newModelExpr);

        var block = Expression.Block(new[] { newModelExpr }, exprs);

        var lambda = Expression.Lambda<ReaderFn<TModel>>(block, entityIdParameter, iteratorParameter, dbParameter);
        return lambda.Compile();
    }

    private delegate void EmitterFn<in TReadModel>(TTransaction tx, TReadModel model)
        where TReadModel : IReadModel;

    internal delegate TReadModel ReaderFn<out TReadModel>(EntityId id, IEnumerator<IReadDatom> iterator, IDb db)
        where TReadModel : IReadModel;
}
