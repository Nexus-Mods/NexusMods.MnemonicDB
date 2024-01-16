using System;
using System.Buffers.Binary;
using System.Linq;
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

        foreach (var staticAttribute in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (staticAttribute.FieldType.IsAssignableTo(typeof(IAttribute)))
            {
                var attributeInstance = staticAttribute.GetValue(null) as IAttribute;
                if (attributeInstance is null)
                {
                    throw new InvalidOperationException($"Attribute {staticAttribute.Name} is null.");
                }

                var indexedAttribute = staticAttribute.GetCustomAttributes<IndexedAttribute>().FirstOrDefault();


                var definition = new AttributeDefinition(attributeInstance, type, attributeInstance.Name, indexedAttribute is not null);
                EntityStructureRegistry.Register(definition);
            }
        }

        EntityStructureRegistry.Register(new EntityDefinition(type, entityAttribute.UUID, entityAttribute.Revision));
        return collection;
    }

}
