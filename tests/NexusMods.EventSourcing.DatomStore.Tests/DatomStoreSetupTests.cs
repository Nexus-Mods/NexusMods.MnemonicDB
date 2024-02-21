using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.DatomStore.Tests;

public class DatomStoreSetupTests(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes)
    : ADatomStoreTest(valueSerializers, attributes)
{
    [Fact]
    public void CanGetAttributesFromDatomStore()
    {

    }
}
