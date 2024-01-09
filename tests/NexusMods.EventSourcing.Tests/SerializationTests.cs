
using System.Buffers;
using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Serialization;
using NexusMods.EventSourcing.TestModel.Events;
using NexusMods.EventSourcing.TestModel.Model;

namespace NexusMods.EventSourcing.Tests;

public class SerializationTests(BinaryEventSerializer serializer)
{

    [Fact]
    public void CanSerializeEvents()
    {


    }


}
