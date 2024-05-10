namespace NexusMods.MnemonicDB.Abstractions.Models;

public class ModelDefinition
{
    /// <summary>
    /// Creates a new ModelDefinition, with the given name.
    /// </summary>
    public static ModelDefinition New(string name)
    {
        return new ModelDefinition();
    }

    public ModelDefinition Composes<TModel>()
    {
        return this;
    }

    public ModelDefinition Composes<TModelA, TModelB>(string name)
    {
        return this;
    }

    public ModelDefinition Composes<TModelA, TModelB, TModelC>(string name)
    {
        return this;
    }

    public ModelDefinition Composes<TModelA, TModelB, TModelC, TModelD>(string name)
    {
        return this;
    }

    public ModelDefinition Composes<TModelA, TModelB, TModelC, TModelD, TModelE>(string name)
    {
        return this;
    }

    /// <summary>
    /// Defines a new attribute on the model of the given attribute type with the given parameters
    /// </summary>
    public ModelDefinition WithAttribute<TAttribute>(string name, bool isIndexed = false, bool noHistory = false)
        where TAttribute : IAttribute
    {
        return this;
    }

    /// <summary>
    /// Defines a reference in another model that points to this class. These references will be exposed
    /// in the `name` property of this model.
    /// </summary>
    public ModelDefinition WithBackReference<TModel>(string name)
    {
        return this;
    }

    public ModelDefinition Build()
    {
        return this;
    }

}
