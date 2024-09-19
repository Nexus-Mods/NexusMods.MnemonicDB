using System;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Occasionally we need to turn the raw datoms from the database into a IReadDatom, this class
/// provides the mappings from AttributeId to IAttribute
/// </summary>
public sealed class AttributeResolver
{
    public IReadDatom Resolve(Datom datom)
    {
        throw new System.NotImplementedException();
    }
    
    /// <summary>
    /// Gets the service object of the specified type.
    /// </summary>
    public IServiceProvider ServiceProvider => throw new System.NotImplementedException();
}
