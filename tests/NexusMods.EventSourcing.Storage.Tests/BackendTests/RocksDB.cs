using NexusMods.EventSourcing.Storage.RocksDbBackend;

namespace NexusMods.EventSourcing.Storage.Tests.BackendTests;

public class RocksDB(IServiceProvider provider) : ABackendTest<Backend>(provider, registry => new Backend(registry)) { }
