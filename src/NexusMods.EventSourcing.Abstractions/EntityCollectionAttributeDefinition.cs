namespace NexusMods.EventSourcing.Abstractions;

/// <inheritdoc />
public class EntityCollectionAttributeDefinition<TOwner, TEntity>(string name) : ACollectionAttribute<TOwner, TEntity>(name) where TOwner : IEntity
    where TEntity : IEntity
{

}
