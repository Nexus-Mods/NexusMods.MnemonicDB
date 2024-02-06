using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Abstractions.BuiltinEntities;
using NexusMods.EventSourcing.Socket;

namespace NexusMods.EventSourcing;

/// <summary>
/// Organizes and gives access to the entity definitions and attribute types and definitions. Essentially all the
/// metadata and reflected information about the entities and their attributes.
/// </summary>
public class EntityRegistry : IEntityRegistry
{
    private readonly IAttributeType[] _attributeTypes;
    private readonly EntityDefinition[] _entityDefinitions;
    private AttributeDefinition[] _attributes;
    private readonly Dictionary<Type,IAttributeType> _byType;
    private readonly Dictionary<Type,EntityDefinition> _entityDefinitionsByType;
    private readonly Dictionary<(UInt128, string), DbRegisteredAttribute> _attributeIds = new();
    private readonly Dictionary<UInt128,EntityDefinition> _entityDefinitionsById;

    private readonly ConcurrentDictionary<(Type, Type), object> _emitters = new();
    private readonly ConcurrentDictionary<(Type, Type), object> _readers = new();

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="entityDefinitions"></param>
    /// <param name="attributeTypes"></param>
    public EntityRegistry(IEnumerable<EntityDefinition> entityDefinitions, IEnumerable<IAttributeType> attributeTypes)
    {
        _attributeTypes = attributeTypes.ToArray();
        _entityDefinitions = entityDefinitions.ToArray();
        _entityDefinitionsByType = _entityDefinitions.ToDictionary(e => e.EntityType);
        _entityDefinitionsById = _entityDefinitions.ToDictionary(e => e.Id);

        _byType = _attributeTypes.ToDictionary(a => a.DomainType);

        var attributes = new List<AttributeDefinition>();

        foreach (var entityDefinition in _entityDefinitions)
        {
            foreach (var attribute in entityDefinition.Attributes)
            {
                if (!_byType.TryGetValue(attribute.NativeType, out var attributeType))
                    throw new InvalidOperationException("Unknown attribute type: " + attributeType + " for attribute " + attribute.Name + " on entity " + entityDefinition.EntityType);
                var definition = new AttributeDefinition()
                {
                    AttributeType = attributeType,
                    EntityTypeId = entityDefinition.Id,
                    Name = attribute.Name,
                    PropertyInfo = attribute.PropertyInfo
                };
                attributes.Add(definition);

            }
        }

        _attributes = attributes.ToArray();
    }


    public AEntity ReadOne<TResultSet>(ref TResultSet resultSet, IDb parentContext) where TResultSet : IResultSet
    {
        if (resultSet.Attribute != (ulong)AttributeIds.EntityTypeId)
            throw new InvalidOperationException("Expected EntityTypeId attribute to be first, got " + resultSet.Attribute);

        if (!_entityDefinitionsById.TryGetValue(resultSet.ValueUInt128, out var entityDefinition))
            throw new InvalidOperationException("Unknown entity type");

        if (!_readers.TryGetValue((entityDefinition.EntityType, typeof(TResultSet)), out var reader))
        {
            reader = MakeReader<TResultSet>(entityDefinition.EntityType);
            _readers.TryAdd((entityDefinition.EntityType, typeof(TResultSet)), reader);
        }
        return ((IEntityRegistry.EntityReader<TResultSet>)reader)(resultSet, parentContext);
    }


    public void EmitOne<TSink>(TSink sink, ulong entityId, AEntity entity, ulong tx) where TSink : IDatomSink
    {
        var typeId = _entityDefinitionsByType[entity.GetType()].Id;
        sink.Emit(entityId, (ulong)AttributeIds.EntityTypeId, typeId, tx);


        if (!_emitters.TryGetValue((entity.GetType(), typeof(TSink)), out var emitter))
        {
            emitter = MakeEmitter<TSink>(entity.GetType());
            _emitters.TryAdd((entity.GetType(), typeof(TSink)), emitter);
        }

        ((IEntityRegistry.EmitEntity<TSink>)emitter)(entity, entityId, tx, ref sink);
    }

    public void PopulateAttributeIds(IEnumerable<DbRegisteredAttribute> attributes)
    {
        foreach (var attribute in attributes)
            _attributeIds.Add((attribute.EntityTypeId, attribute.Name), attribute);

    }

    public ulong TransactAttributeChanges(IDatomStore store, ulong nextTx)
    {
        var missingAttributes = new List<DbRegisteredAttribute>();

        var maxId = _attributeIds.Values.Max(x => x.Id);

        foreach (var entity in _entityDefinitions)
        {
            foreach (var attribute in entity.Attributes)
            {
                if (!_attributeIds.ContainsKey((entity.Id, attribute.Name)))
                {
                    var typeDefinition = _byType[attribute.NativeType];
                    missingAttributes.Add(new DbRegisteredAttribute
                    {
                        EntityTypeId = entity.Id,
                        Name = attribute.Name,
                        ValueType = typeDefinition.ValueType,
                        Id = ++maxId
                    });
                }

            }
        }

        if (missingAttributes.Count == 0)
            return nextTx;

        var socket = new NewAttributeSinkSocket(missingAttributes, nextTx);
        ulong nextId = 0;
        store.Transact(ref socket, ref nextId, new Dictionary<ulong, ulong>());
        PopulateAttributeIds(missingAttributes);
        return nextTx + 1;
    }


    public IEntityRegistry.EmitEntity<TSink> MakeEmitter<TSink>(Type entityType) where TSink : IDatomSink
    {
        var entityDefinition = _entityDefinitions.First(e => e.EntityType == entityType);

        var entityParam = Expression.Parameter(typeof(AEntity), "entity");
        var entityCasted = Expression.Variable(entityType,"entityCasted");
        var eParam = Expression.Parameter(typeof(ulong), "e");
        var tParam = Expression.Parameter(typeof(ulong), "t");
        var sinkParam = Expression.Parameter(typeof(TSink).MakeByRefType(), "sink");

        var body = new List<Expression>();
        body.Add(Expression.Assign(entityCasted, Expression.Convert(entityParam, entityType)));
        foreach (var attr in entityDefinition.Attributes)
        {
            var attrType = _byType[attr.NativeType];
            var emitMethod = attrType.GetType().GetMethod("Emit")!.MakeGenericMethod(typeof(TSink));
            var attribute = _attributeIds[(entityDefinition.Id, attr.Name)];
            var valExpr = Expression.Property(entityCasted, attr.PropertyInfo);
            var emitCall = Expression.Call(Expression.Constant(attrType), emitMethod, eParam, Expression.Constant(attribute.Id), valExpr, tParam, sinkParam);
            body.Add(emitCall);

        }

        var bodyExpr = Expression.Block([entityCasted], body);

        var lambda = Expression.Lambda<IEntityRegistry.EmitEntity<TSink>>(bodyExpr, entityParam, eParam, tParam, sinkParam);

        return lambda.Compile();
    }


    public IEntityRegistry.EntityReader<TResultSet> MakeReader<TResultSet>(Type entityType) where TResultSet : IResultSet
    {
        var entityId = _entityDefinitionsByType[entityType];
        var attributes = _entityDefinitionsByType[entityType]
            .Attributes
            .Select(a => new
            {
                DBAttribute = _attributeIds[(entityId.Id, a.Name)],
                Definition = a
            })
            .OrderBy(a => a.DBAttribute.Id);

        var resultSetParam = Expression.Parameter(typeof(TResultSet), "resultSet");
        var dbParam = Expression.Parameter(typeof(IDb), "db");

        var exprs = new List<Expression>();
        var startingId = Expression.Parameter(typeof(UInt64), "startingId");
        exprs.Add(Expression.Assign(startingId, Expression.Property(resultSetParam, "EntityId")));

        var newParam = Expression.Parameter(entityType, "newEntity");
        var ctor = entityType
            .GetConstructors()
            .First(c => c.GetParameters().Length == 2);

        var entityIdExpr = Expression.Call(typeof(EntityId), "From", null, startingId);

        exprs.Add(Expression.Assign(newParam, Expression.New(ctor, dbParam, entityIdExpr)));

        var topLabel = Expression.Label("Top");
        exprs.Add(Expression.Label(topLabel));

        var switchCases = new List<SwitchCase>();

        var endLabel = Expression.Label("SwitchEnd");
        var returnLabel = Expression.Label("ReturnLabel");

        foreach (var entry in attributes)
        {
            var bodyBlock = new List<Expression>();

            var attributeType = _byType[entry.Definition.NativeType];
            var readMethod = attributeType.GetType().GetMethod("GetValue", BindingFlags.Public | BindingFlags.Instance)!.MakeGenericMethod(typeof(TResultSet));
            var readCall = Expression.Call(Expression.Constant(attributeType), readMethod, resultSetParam, dbParam);
            bodyBlock.Add(Expression.Assign(Expression.Property(newParam, entry.Definition.PropertyInfo), readCall));
            bodyBlock.Add(Expression.Break(endLabel));

            switchCases.Add(Expression.SwitchCase(Expression.Block(bodyBlock), Expression.Constant(entry.DBAttribute.Id)));
        }


        var switchExpr = Expression.Switch(Expression.Property(resultSetParam, "Attribute"),
            Expression.Block(Expression.Break(endLabel)), switchCases.ToArray());

        exprs.Add(switchExpr);

        exprs.Add(Expression.Label(endLabel));

        exprs.Add(Expression.IfThen(Expression.Not(Expression.Call(resultSetParam, "Next", null)),
            Expression.Return(returnLabel)));

        exprs.Add(Expression.IfThen(Expression.Equal(startingId, Expression.Property(resultSetParam, "EntityId")),
            Expression.Goto(topLabel)));

        exprs.Add(Expression.Label(returnLabel));
        exprs.Add(newParam);
        var body = Expression.Block([startingId, newParam], exprs);

        var lambda = Expression.Lambda<IEntityRegistry.EntityReader<TResultSet>>(body, resultSetParam, dbParam).Compile();

        return lambda;
    }


}
