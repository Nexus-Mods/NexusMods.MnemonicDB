using NexusMods.MnemonicDB.Storage.RocksDbBackend;

namespace NexusMods.MnemonicDB.Storage.Tests.BackendTests;

public class RocksDB(IServiceProvider provider) : ABackendTest<Backend>(provider, registry => new Backend(registry)) { }
