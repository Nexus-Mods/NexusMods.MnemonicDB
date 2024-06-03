namespace NexusMods.MnemonicDB.Abstractions.Models;

/// <summary>
/// A interface for the concrete implementation of a readonly model.
/// </summary>
/// <typeparam name="TInterface"></typeparam>
/// <typeparam name="TModel"></typeparam>
public interface IReadOnlyConcreteModel<TInterface, TModel>
where TModel : ReadOnlyModel, TInterface
{
    /// <summary>
    /// Create a new instance of the ReadOnlyModel with the given db and id.
    /// </summary>
    public static abstract TInterface CreateReadOnly(IDb db, EntityId id);

    /// <summary>
    /// List of attributes that are required for this model.
    /// </summary>
    public static abstract IAttribute[] RequiredAttributes { get; }
}
