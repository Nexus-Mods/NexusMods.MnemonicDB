// ReSharper disable CheckNamespace

using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Cascade;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Interface for a specific attribute
/// </summary>
public abstract partial class Attribute<TValueType, TLowLevelType, TSerializer> : IDiffFlow<TValueType> 
    where TValueType : notnull
    where TSerializer : IValueSerializer<TLowLevelType>
{
    public static int operator ==(Attribute<TValueType, TLowLevelType, TSerializer> a, TValueType value)
    {
        throw new NotImplementedException();
    }

    public static int operator !=(Attribute<TValueType, TLowLevelType, TSerializer> a, TValueType value)
    {
        throw new NotImplementedException();
    }

    public FlowDescription AsFlow()
    {
        return this.QueryAll().AsFlow();
    }
}
