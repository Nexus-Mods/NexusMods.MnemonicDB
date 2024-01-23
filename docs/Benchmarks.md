
## Benchmarks
Here are some benchmark results for various parts of the system. All benchmarks were performed on a SteamDeck:

```
              .,,,,.                  deck@steamdeck
        .,'onNMMMMMNNnn',.            --------------
     .'oNMANKMMMMMMMMMMMNNn'.         OS: SteamOS Holo x86_64
   .'ANMMMMMMMXKNNWWWPFFWNNMNn.       Host: Jupiter 1
  ;NNMMMMMMMMMMNWW'' ,.., 'WMMM,      Kernel: 5.13.0-valve37-1-neptune
 ;NMMMMV+##+VNWWW' .+;'':+, 'WMW,     Uptime: 28 mins
,VNNWP+######+WW,  +:    :+, +MMM,    Packages: 925 (pacman), 13 (brew), 12 (flatpak)
'+#############,   +.    ,+' +NMMM    Shell: bash 5.1.16
  '*#########*'     '*,,*' .+NMMMM.   Resolution: 800x1280
     `'*###*'          ,.,;###+WNM,   Terminal: /dev/pts/2
         .,;;,      .;##########+W    CPU: AMD Custom APU 0405 (8) @ 2.800GHz
,',.         ';  ,+##############'    GPU: AMD ATI AMD Custom GPU 0405
 '###+. :,. .,; ,###############'     Memory: 5341MiB / 14818MiB
  '####.. `'' .,###############'
    '#####+++################'
      '*##################*'
         ''*##########*''
              ''''''
```

## Write

This shows the amount of time taken (and memory allocated) to write the `EventCount` number of events to the event store. With
RocksDB, the average time for Rocks DB is roughly 12 microseconds or roughly 83,000 events per second.

| Method      | EventStoreType                            | EventCount | Mean         | Error       | StdDev      | Median       | Allocated  |
|------------ |------------------------------------------ |----------- |-------------:|------------:|------------:|-------------:|-----------:|
| WriteEvents | RocksDBEventStore<BinaryEventSerializer>  | 100        |   2,416.7 us |   129.35 us |   371.13 us |   2,302.8 us |    1.78 KB |
| WriteEvents | RocksDBEventStore<BinaryEventSerializer>  | 1000       |  13,644.9 us | 1,072.56 us | 3,128.71 us |  13,263.6 us |    1.78 KB |
| WriteEvents | RocksDBEventStore<BinaryEventSerializer>  | 10000      | 123,009.5 us | 2,409.70 us | 6,875.00 us | 123,518.4 us |    1.78 KB |
| WriteEvents | InMemoryEventStore<BinaryEventSerializer> | 100        |     502.8 us |    22.73 us |    63.73 us |     477.0 us |   23.87 KB |
| WriteEvents | InMemoryEventStore<BinaryEventSerializer> | 1000       |   4,676.9 us |   176.09 us |   516.46 us |   4,669.0 us |  211.38 KB |
| WriteEvents | InMemoryEventStore<BinaryEventSerializer> | 10000      |   9,918.0 us |   194.39 us |   199.63 us |   9,916.3 us | 2185.84 KB |

## Read
This benchmark shows the time taken to read the `EventCount` number of events from the event store, when there are `EntityCount` number of entities
in the store (each with `EventCount` number of events). With RocksDB, the average time for Rocks DB to load a single event is 8.7 microseconds.
Or roughly 115,000 events per second. Note that reads can be done in parallel so that number is per-core. Also note that snapshotting of events
is not performed in this benchmark.

| Method     | EventStoreType                           | EventCount | EntityCount | Mean        | Error       | StdDev      | Allocated  |
|----------- |----------------------------------------- |----------- |------------ |------------:|------------:|------------:|-----------:|
| ReadEvents | RocksDBEventStore<BinaryEventSerializer> | 100        | 100         |    172.3 us |     3.40 us |     3.18 us |    9.56 KB |
| ReadEvents | RocksDBEventStore<BinaryEventSerializer> | 100        | 1000        |    206.0 us |     3.78 us |     3.88 us |   10.27 KB |
| ReadEvents | RocksDBEventStore<BinaryEventSerializer> | 1000       | 100         |  2,716.8 us |    14.05 us |    12.46 us |  100.97 KB |
| ReadEvents | RocksDBEventStore<BinaryEventSerializer> | 1000       | 1000        |  4,268.1 us |    75.23 us |    66.69 us |  101.68 KB |
| ReadEvents | RocksDBEventStore<BinaryEventSerializer> | 10000      | 100         | 64,665.7 us | 1,253.08 us | 1,715.23 us | 1015.12 KB |
| ReadEvents | RocksDBEventStore<BinaryEventSerializer> | 10000      | 1000        | 87,726.5 us | 1,127.58 us |   999.57 us | 1015.85 KB |

## Event Serialization
This benchmark shows the amount of time taken to serialize 7 events of various types (create loadout, rename loadout, etc).
All these numbers should be divided by 7 to get the per-event time. Thus the serialization rate here is 16.3 million events per second,
while the deserialize rate is 14.6 million events per second. Again, all these numbers are per-core.

!!!info
   No memory is allocated during serialization due to memory pooling.

| Method      | Mean     | Error   | StdDev  | Gen0   | Allocated |
|------------ |---------:|--------:|--------:|-------:|----------:|
| Serialize   | 428.7 ns | 2.95 ns | 2.76 ns |      - |         - |
| Deserialize | 477.1 ns | 3.69 ns | 3.27 ns | 0.0067 |     592 B |
