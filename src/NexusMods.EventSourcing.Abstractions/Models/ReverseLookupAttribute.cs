using System;

namespace NexusMods.EventSourcing.Abstractions.Models;

/// <summary>
/// Defines a backwards lookup attribute
/// </summary>
public class ReverseLookupAttribute<TAttribute> : Attribute
where TAttribute : ScalarAttribute<TAttribute, EntityId>
{

}
