using System;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Marks a attribute definition as being indexed.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IndexedAttribute : Attribute
{
    public IndexedAttribute()
    {
    }
}
