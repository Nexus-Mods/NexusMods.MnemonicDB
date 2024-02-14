namespace NexusMods.EventSourcing.Abstractions.ModelGeneration;

/// <summary>
/// Placeholder class for generating model definitions for source generators
/// </summary>
public class ModelDefinitionBuilder
{
    /// <summary>
    /// Placeholder class for generating model definitions for source generators
    /// </summary>
    public ModelDefinitionBuilder()
    {

    }


    /// <summary>
    /// Includes the attribute in the model definition
    /// </summary>
    /// <typeparam name="TAttr"></typeparam>
    /// <returns></returns>
    public ModelDefinitionBuilder Include<TAttr>()
    where TAttr : IAttribute
    {
        return this;
    }

    /// <summary>
    /// Builds the definition into a model with the given name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public ModelDefinition Build(string name)
    {
        return new ModelDefinition();
    }
}
