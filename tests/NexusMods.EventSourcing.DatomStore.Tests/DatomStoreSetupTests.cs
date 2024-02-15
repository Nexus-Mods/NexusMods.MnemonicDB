using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.DatomStore.Tests;

public class DatomStoreSetupTests : ADatomStoreTest
{
    public DatomStoreSetupTests(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes)
        : base(valueSerializers, attributes)
    {
    }

    [Fact]
    public void CanGetAttributesFromDatomStore()
    {

    }
}
