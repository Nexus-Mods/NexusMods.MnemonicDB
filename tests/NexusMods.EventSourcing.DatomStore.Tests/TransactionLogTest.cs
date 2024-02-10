using NexusMods.EventSourcing.Abstractions;

namespace NexusMods.EventSourcing.DatomStore.Tests;

public class TransactionLogTest
{
    private readonly IEnumerable<IAttribute> _attrs;

    public TransactionLogTest(IEnumerable<IAttribute> attrs)
    {
        _attrs = attrs;
    }

    [Fact]
    public void BasicTest()
    {

    }

}
