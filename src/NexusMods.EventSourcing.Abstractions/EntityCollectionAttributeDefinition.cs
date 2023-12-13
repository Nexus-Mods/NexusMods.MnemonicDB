namespace NexusMods.EventSourcing.Abstractions;

public class EntityCollectionAttributeDefinition<TOwner, TEntity>(string name) : ACollectionAttribute<TOwner, TEntity>(name) where TOwner : IEntity
    where TEntity : IEntity
{

}
