namespace NexusMods.MnemonicDB.Abstractions.Models;

public interface IModel
{
    public EntityId Id { get; set; }

    /// <summary>
    /// Set the value of an attribute to the given value
    /// </summary>
    public void Add<TOuter, TInner>(Attribute<TOuter, TInner> attribute, TOuter value);
}
