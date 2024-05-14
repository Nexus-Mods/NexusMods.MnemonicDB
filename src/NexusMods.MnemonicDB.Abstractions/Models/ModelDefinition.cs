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
    public ModelDefinition Attribute<TType>(string name, bool isIndexed = false, bool noHistory = false)
    where TType : IAttribute
    {
        return this;
    }

    /// <summary>
    /// Define a reference to another model, via an attribute of the given name.
    /// </summary>
    public ModelDefinition Reference<TModel>(string name)
    {
        return this;
    }

    /// <summary>
    /// Define a multi-cardinality reference to another model, via an attribute of the given name.
    /// </summary>
    public ModelDefinition References<TModel>(string name)
    {
        return this;
    }

    /// <summary>
    /// Define an attribute that is a marker; it doesn't have a value, its existance determines if the value
    /// is true or false.
    /// </summary>
    public ModelDefinition MarkedBy(string name)
    {
        return this;
    }

    /// <summary>
    /// Defines a reference in another model that points to this class. These references will be exposed
    /// in the `name` property of this model.
    /// </summary>
    public ModelDefinition BackRef<TModel>(string name)
    {
        return this;
    }

    public ModelDefinition Build()
    {
        return this;
    }

}
