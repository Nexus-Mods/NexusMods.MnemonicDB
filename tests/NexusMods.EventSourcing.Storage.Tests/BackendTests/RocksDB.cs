using NexusMods.EventSourcing.Abstractions;
using NexusMods.EventSourcing.Storage.RocksDbBackend;
using NexusMods.EventSourcing.TestModel.ComplexModel.Attributes;
using NexusMods.EventSourcing.TestModel.Helpers;
using NexusMods.Hashing.xxHash64;
using FileAttributes = NexusMods.EventSourcing.TestModel.ComplexModel.Attributes.FileAttributes;

namespace NexusMods.EventSourcing.Storage.Tests.BackendTests;

public class RocksDB(IServiceProvider provider) : ABackendTest<Backend>(provider, (registry) => new Backend(registry))
{


}
