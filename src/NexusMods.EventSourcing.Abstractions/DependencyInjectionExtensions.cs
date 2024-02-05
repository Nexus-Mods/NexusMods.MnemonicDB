using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.EventSourcing.Abstractions;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers an attribute type with the service collection
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddAttributeType<T>(this IServiceCollection services) where T : class, IAttributeType
    {
        services.AddSingleton<IAttributeType, T>();
        return services;
    }


    /// <summary>
    /// Registers an entity with the service collection
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IServiceCollection AddEntity<T>(this IServiceCollection services)
    {

        var entityType = typeof(T);
        var entityTypeId = entityType.GetCustomAttribute<EntityAttribute>()?.Id ??
                       throw new InvalidOperationException($"Entity {entityType.Name} does not have an EntityIdAttribute");

        // TODO: Support polymorphic entities
        var attributes = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        var attributeDefinitions = new List<EntityAttributeDefinition>();
        foreach (var attribute in attributes)
        {
            var attributeType = attribute.PropertyType;
            if (!attribute.CanRead)
                continue;
            attributeDefinitions.Add(new EntityAttributeDefinition(attribute.Name, attributeType));
        }

        services.AddSingleton(new EntityDefinition(entityTypeId, entityType, attributeDefinitions.ToArray()));

        return services;
    }

}
