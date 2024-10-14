# NexusMods.MnemonicDB

This is a simple, fast, and type-safe in-process temporal database for .NET applications. It borrows heavily from
[Datomic](https://www.datomic.com/), [Datahike](https://github.com/replikativ/datahike) and heavily leverages
[RocksDB](https://rocksdb.org/) for storage. Performance wise, on modern desktop hardware it can write roughly
500kK tuples per second sustained, and read roughly 1M tuples per second sustained, this includes the overhead
of maintaining the indexes.

## Documentation

The full docs can be found [here](https://nexus-mods.github.io/NexusMods.MnemonicDB/)

## License

See [LICENSE.md](./LICENSE.md)
