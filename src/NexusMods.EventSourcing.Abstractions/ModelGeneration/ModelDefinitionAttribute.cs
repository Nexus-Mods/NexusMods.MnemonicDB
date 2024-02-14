using System;

namespace NexusMods.EventSourcing.Abstractions.ModelGeneration;

/// <summary>
/// Marks a class as a model definition
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ModelDefinitionAttribute : Attribute;
