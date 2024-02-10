using System;
using System.Runtime.CompilerServices;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Interface for a specific attribute
/// </summary>
/// <typeparam name="TValueType"></typeparam>
public class ScalarAttribute<TValueType> : IAttribute
{
    /// <summary>
    /// Create a new attribute
    /// </summary>
    /// <param name="guid"></param>
    protected ScalarAttribute(string guid)
    {
        Id = guid.ToUInt128Guild();
        var fullName = GetType().FullName;
        var splitOn = fullName!.LastIndexOf('.');
        Name = fullName[(splitOn + 1)..];
        Namespace = fullName[..splitOn];
    }

    /// <summary>
    /// Create a new attribute from an already parsed guid
    /// </summary>
    /// <param name="guid"></param>
    protected ScalarAttribute(UInt128 guid)
    {
        Id = guid;
        var fullName = GetType().FullName;
        var splitOn = fullName!.LastIndexOf('.');
        Name = fullName[(splitOn + 1)..];
        Namespace = fullName[..splitOn];
    }

    /// <inheritdoc />
    public Type ValueType => typeof(TValueType);

    /// <inheritdoc />
    public bool IsMultiCardinality => false;

    /// <inheritdoc />
    public bool IsReference => false;

    /// <inheritdoc />
    public UInt128 Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Namespace { get; }
}
