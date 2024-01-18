using System;
using System.Buffers.Binary;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// DI extensions for the event sourcing library.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers an event with the service collection.
    /// </summary>
    /// <param name="collection"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddEvent<T>(this IServiceCollection collection) where T : class, IEvent
    {
        var type = typeof(T);
        var attribute = type.GetCustomAttribute<EventIdAttribute>();
        if (attribute is null)
        {
            throw new ArgumentException($"Event type {type.Name} does not have an EventIdAttribute.");
        }

        Span<byte> span = stackalloc byte[16];
        attribute.Guid.TryWriteBytes(span);
        var id = BinaryPrimitives.ReadUInt128BigEndian(span);

        collection.AddSingleton(s => new EventDefinition(id, type));
        return collection;
    }


    /// <summary>
    /// Registers an entity with the service collection, this is required for loading snapshots and properly
    /// tracking entity revisions in the application
    /// </summary>
    /// <param name="collection"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IServiceCollection AddEntity<T>(this IServiceCollection collection) where T : class, IEntity
    {
        var type = typeof(T);
        var entityAttribute = type.GetCustomAttribute<EntityAttribute>();
        if (entityAttribute is null)
        {
            throw new ArgumentException($"Entity type {type.Name} does not have an EntityAttribute.");
        }

        EntityStructureRegistry.Register(IEntity.EntityIdAttribute);

        foreach (var attributeDefinition in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
        {
            if (attributeDefinition.GetValue(null) is not IAttribute attributeInstance)
            {
                continue;
            }

            var property = type.GetProperty(attributeInstance.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property is null)
            {
                throw new Exception($"Property {attributeInstance.Name} not found on entity {type.Name} as required by attribute {attributeDefinition.Name}");
            }

            var indexedAttribute = property.GetCustomAttribute<IndexedAttribute>();

            if (indexedAttribute is not null)
            {
                if (attributeInstance is IIndexableAttribute indexableAttributeInstance)
                {
                    indexableAttributeInstance.IndexedAttributeId = indexedAttribute.Id;
                }
                else
                {
                    throw new Exception($"Attribute {attributeInstance.Name} on entity {type.Name} is not indexable");
                }
            }

            EntityStructureRegistry.Register(attributeInstance);
        }


        EntityStructureRegistry.Register(new EntityDefinition(type, entityAttribute.UUID, entityAttribute.Revision));
        return collection;
    }

}
