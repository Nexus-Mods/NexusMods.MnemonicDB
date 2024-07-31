using System;
using System.Buffers;
using System.Collections.Generic;

namespace NexusMods.MnemonicDB.Abstractions.Internals;

/// <summary>
///     A registry of attributes and serializers that supports operations that requires converting
///     between the database IDs, the code-level attributes and the native values
/// </summary>
public interface IAttributeRegistry
{
    /// <summary>
    ///     Resolve the given KeyPrefix + Value into a datom
    /// </summary>
    /// <param name="datom"></param>
    /// <returns></returns>
    public IReadDatom Resolve(in KeyPrefix prefix, ReadOnlySpan<byte> datom);
    
    
    /// <summary>
    /// Resolve the given attribute id into an attribute
    /// </summary>
    public IAttribute GetAttribute(AttributeId attributeId);

    /// <summary>
    /// Populates the registry with the given attributes, mostly used for
    /// internal registration of attributes
    /// </summary>
    /// <param name="attributes"></param>
    public void Populate(IEnumerable<DbAttribute> attributes);

    /// <summary>
    /// The registry id of the registry, this can be used to link attributes to attribute ids.
    /// A separate registry id is used for each registry instance and backing datom store.
    /// </summary>
    public RegistryId Id { get; }
    
    /// <summary>
    /// This is used by various attributes who need a service provider specific to a registry
    /// </summary>
    public IServiceProvider ServiceProvider { get; }
}

/// <summary>
/// No it's not a AbstractSingletonProxyFactoryBean, it's registry of attribute registries
/// </summary>
public static class AttributeRegistryRegistry
{
    /// <summary>
    /// All the registries currently active for this program, noramlly this is only one, but during tests, database
    /// migrations or the like, there may be more than one. This is used so that we can pass around a very small number
    /// (A byte) to reference the correct registry, as well as globally look up all the registries. Writing to this
    /// collection should never be done outside of the AttributeRegistry class. Reading can be done by anyone who has a
    /// valid registry id.
    /// </summary>
    public static readonly IAttributeRegistry?[] Registries = new IAttributeRegistry[8];
}
