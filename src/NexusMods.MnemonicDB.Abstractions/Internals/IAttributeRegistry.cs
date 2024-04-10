using System;
using System.Buffers;

namespace NexusMods.MnemonicDB.Abstractions.Internals;

/// <summary>
///     A registry of attributes and serializers that supports operations that requires converting
///     between the database IDs, the code-level attributes and the native values
/// </summary>
public interface IAttributeRegistry
{
    /// <summary>
    ///     Compares the given values in the given spans assuming both are tagged with the given attribute
    /// </summary>
    public int CompareValues(AttributeId id, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);

    /// <summary>
    ///     Resolve the given KeyPrefix + Value into a datom
    /// </summary>
    /// <param name="datom"></param>
    /// <returns></returns>
    public IReadDatom Resolve(ReadOnlySpan<byte> datom);

    /// <summary>
    /// Assumes the given datom is a value of the given type and deserializes it
    /// </summary>
    public TVal Resolve<TVal>(ReadOnlySpan<byte> datom);

    /// <summary>
    /// Populates the registry with the given attributes, mostly used for
    /// internal registration of attributes
    /// </summary>
    /// <param name="attributes"></param>
    public void Populate(DbAttribute[] attributes);

    /// <summary>
    /// The registry id of the registry, this can be used to link attributes to attribute ids.
    /// A separate registry id is used for each registry instance and backing datom store.
    /// </summary>
    public RegistryId Id { get; }
}
