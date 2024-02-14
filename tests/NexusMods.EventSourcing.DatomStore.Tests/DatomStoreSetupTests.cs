using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.DatomStore.Tests;

public class DatomStoreSetupTests : ADatomStoreTest
{
    public DatomStoreSetupTests(IEnumerable<IValueSerializer> valueSerializers, IEnumerable<IAttribute> attributes,
        IEnumerable<IReadModelFactory> factories)
        : base(valueSerializers, attributes, factories)
    {
    }

    [Fact]
    public void CanGetAttributesFromDatomStore()
    {

    }
}
