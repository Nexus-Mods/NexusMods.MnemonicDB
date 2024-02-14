namespace NexusMods.EventSourcing.Abstractions.ModelGeneration;

/// <summary>
/// Placeholder for a model definition for source generators
/// </summary>
public class AttributeDefinitionsBuilder
{
    /// <summary>
    /// Placeholder for a model definition for source generators
    /// </summary>
    public AttributeDefinitionsBuilder()
    {

    }

    /// <summary>
    /// Defines a new attribute with the given name and description
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <typeparam name="TValueType"></typeparam>
    /// <returns></returns>
    public AttributeDefinitionsBuilder Define<TValueType>(string name, string description)
    {
        return this;
    }

    /// <summary>
    /// Defines a new attribute with the given name and description
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <typeparam name="ValueType"></typeparam>
    /// <returns></returns>
    public AttributeDefinitionsBuilder Include<TAttr>()
    where TAttr : IAttribute
    {
        return this;
    }

    /// <summary>
    /// Builds the final model definition
    /// </summary>
    /// <returns></returns>
    public AttributeDefinitions Build()
    {
        return AttributeDefinitions.Instance;
    }

}
