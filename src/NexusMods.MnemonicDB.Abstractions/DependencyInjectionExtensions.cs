using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions.Models;

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
    public static IServiceCollection AddModel<T>(this IServiceCollection services)
        where T : IModel
    {
        var type = typeof(T);
        if (!type.IsInterface)
            throw new ArgumentException("The type must be an interface.", nameof(type));

        var propertyClass = (Type?)type.GetMembers().FirstOrDefault(m => m.Name == "Attributes");
        if (propertyClass is null)
            throw new InvalidOperationException($"Model {type.FullName ?? type.Name} does not have an Attributes inner class");

        var attributes = propertyClass.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType.IsAssignableTo(typeof(IAttribute)));

        foreach (var attribute in attributes)
        {
            var field = attribute.GetValue(null);
            if (field is not IAttribute casted)
                throw new ArgumentException("The field must be of type IAttribute.", nameof(type));
            services.AddSingleton(typeof(IAttribute), casted);
        }

        return services;
    }

    /// <summary>
    ///     Assumes that the specified type is a static class with nested attribute classes, it registers all the nested
    ///     classes with the service collection.
    /// </summary>
    public static IServiceCollection AddAttributeCollection(this IServiceCollection services, Type type)
    {
        if (!type.IsClass)
            throw new ArgumentException("The type must be a class.", nameof(type));


        var attributes = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType.IsAssignableTo(typeof(IAttribute)));

        foreach (var attribute in attributes)
        {
            var field = attribute.GetValue(null);
            if (field is not IAttribute casted)
                throw new ArgumentException("The field must be of type IAttribute.", nameof(type));
            services.AddSingleton(typeof(IAttribute), casted);
        }

        return services;
    }
}
