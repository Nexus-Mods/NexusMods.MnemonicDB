using System;
using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing;

public static class InitialData
{
    public static UInt128 AttributeEntityDefinition = 1;

    public static IAttribute<string> AttributeName = new Attribute<string>(AttributeEntityDefinition, "Name");

    public static void Add<TTransaction>(TTransaction tx) where TTransaction : ITransaction
    {

        AttributeName.Emit(EntityId.From(1), "Name", tx);
    }


}
