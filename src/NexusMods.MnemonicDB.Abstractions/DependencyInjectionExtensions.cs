using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Extension methods for adding attributes and other types to the service collection.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    ///     Registers the specified attribute type with the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TAttribute"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddAttribute<TAttribute>(this IServiceCollection services)
        where TAttribute : class, IAttribute
    {
        services.AddSingleton<IAttribute, TAttribute>();
        return services;
    }

    /// <summary>
    ///     Assumes that the specified type is a static class with nested attribute classes, it registers all the nested
    ///     classes with the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TAttributeCollection"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IServiceCollection AddAttributeCollection<TAttributeCollection>(this IServiceCollection services)
        where TAttributeCollection : class
    {
        var type = typeof(TAttributeCollection);

        if (!type.IsClass)
            throw new ArgumentException("The type must be a class.", nameof(TAttributeCollection));

        var attributes = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType.IsAssignableTo(typeof(IAttribute)));

        foreach (var attribute in attributes)
        {
            var field = attribute.GetValue(null);
            if (field is not IAttribute casted)
                throw new ArgumentException("The field must be of type IAttribute.", nameof(TAttributeCollection));
            services.AddSingleton(typeof(IAttribute), casted);
        }

        return services;
    }


    /// <summary>
    ///     Adds the value serializer to the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TValueSerializer"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddValueSerializer<TValueSerializer>(this IServiceCollection services)
        where TValueSerializer : class, IValueSerializer
    {
        services.AddSingleton<IValueSerializer, TValueSerializer>();
        return services;
    }
}
