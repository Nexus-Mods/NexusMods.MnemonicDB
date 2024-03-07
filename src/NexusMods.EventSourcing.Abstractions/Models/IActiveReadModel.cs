using System.ComponentModel;

namespace NexusMods.EventSourcing.Abstractions.Models;

/// <summary>
/// A read model that automatically updates as new commits are made to the database that modify its state.
/// </summary>
public interface IActiveReadModel : INotifyPropertyChanged
{
    /// <summary>
    /// The unique identifier of the entity.
    /// </summary>
    public EntityId Id { get; }
}
