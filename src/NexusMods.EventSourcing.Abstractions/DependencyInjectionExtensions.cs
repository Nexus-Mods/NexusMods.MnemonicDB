using System;
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
        collection.AddSingleton(s => new EventDefinition(attribute.Guid, type));
        return collection;
    }

}
