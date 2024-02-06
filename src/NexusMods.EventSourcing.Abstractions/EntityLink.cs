namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A lazy link to another entity
/// </summary>
public struct EntityLink<T> where T : AEntity
{
    private readonly EntityId<T> _id;
    private readonly IDb _db;

    public EntityId Id => _id;

    public EntityLink(EntityId<T> id, IDb db)
    {
        _id = id;
        _db = db;
    }

    public T Value => _db.Get(_id);


    /// <summary>
    /// Implicitly convert from a entity to a link
    /// </summary>
    /// <param name="link"></param>
    /// <returns></returns>
    public static implicit operator EntityLink<T>(T link) => new(EntityId<T>.From(link.Id), link.Context);

}
