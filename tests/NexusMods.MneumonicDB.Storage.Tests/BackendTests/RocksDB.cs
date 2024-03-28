using NexusMods.MneumonicDB.Storage.RocksDbBackend;

namespace NexusMods.MneumonicDB.Storage.Tests.BackendTests;

public class RocksDB(IServiceProvider provider) : ABackendTest<Backend>(provider, registry => new Backend(registry)) { }
