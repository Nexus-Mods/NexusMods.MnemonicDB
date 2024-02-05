using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
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

        _byType = _attributeTypes.ToDictionary(a => a.DomainType);

        var attributes = new List<AttributeDefinition>();

        foreach (var entityDefinition in _entityDefinitions)
        {
            foreach (var attribute in entityDefinition.Attributes)
            {
                var attributeType = _byType[attribute.NativeType];
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
        store.Transact(ref socket);
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

}
