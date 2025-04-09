// ReSharper disable CheckNamespace

using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.MnemonicDB.Abstractions.Cascade;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     Interface for a specific attribute
/// </summary>
public abstract partial class Attribute<TValueType, TLowLevelType, TSerializer> : IDiffFlow<EVRow<TValueType>> 
{
    public ISource<DiffSet<EVRow<TValueType>>> ConstructIn(ITopology topology)
    {
        var flow = NexusMods.MnemonicDB.Abstractions.Cascade.Query.Updates.ForAttribute(this);
        return topology.Intern(flow);
    }

    public static int operator ==(Attribute<TValueType, TLowLevelType, TSerializer> a, TValueType value)
    {
        throw new NotImplementedException();
    }

    public static int operator !=(Attribute<TValueType, TLowLevelType, TSerializer> a, TValueType value)
    {
        throw new NotImplementedException();
    }
}
