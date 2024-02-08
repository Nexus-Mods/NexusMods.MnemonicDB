---
hide:
  - toc
---

## Datom Store
A key part of this event sourcing framework is a storage system for efficent storage and retrieval of event datoms. Each modification
of the system is stored in a transaction, and each transaction is a set of datoms. These datoms are in the format of `[Entity, Attribute, Value, Tx]`.

The `Entity` is a unique identifier for the entity at hand, and is a 64-bit unsigned integer. The attribute is a unique identifier
for an attribute that is itself a 64-bit unsigned integer. Interestingly the attribute is also an entity, and can have its own attributes and associated
metadata. The `Value` is the value of the attribute, and can be a string, integer, float, or any number of other value types. The `Tx` is a unique identifier
for the transaction that the datom is a part of and is a monotonic increasing 64-bit unsigned integer. The `Tx` is also a unique identifier for the transaction
entity, and can have its own attributes and associated metadata.

At a basic level the retrieval process for a datom is simple: the ordered list of datoms is filtered by the `Entity` and `Attribute` and the value associated
with the most recent `Tx` is returned. This is a simple and efficient process, and is the basis for the entire system. However, as expected, a O(n) search
over a list of a several million datoms is not efficient. To solve this problem, the datoms are indexed in various methods to allow for efficient retrieval.

### Is this Datomic?
This system is heavily inspired by Datomic, but is not a direct copy. The primary difference is that Datomic is a distributed database and this system
is a single node in-process database. This gives us several advantages, such as the ability to use the full power of C# and .NET, and the ability to
reduce concerns about network latency, failure, and failover. It should be noted that no one involved in this process has ever seen the source code for Datomic,
and the design of this system is based on the publicly available information about Datomic. It should also be noted that this product is in no way
affiliated with Datomic or its creators.


### Id Partitioning
This system makes heavy use of ID partitioning to encode extra information about a given entity id into the id itself. This is done by using the high bits of the
64-bit entity id to encode the partition, and the low bits to encode the actual entity id. This allows for efficient transmission of the partition information. Some common
partitions prefixes are:

- Attributes
- Multi-cardinality attributes
- References (attributes whos value points to another entity)
- Multi-cardinality references
- Transactions
- Entity ids
- Tempids (temporary entity ids used during transaction processing)

Since we are using the first byte of the entity id to encode the partition, we have 256 partitions available to us. This is more than enough for the foreseeable future, and even allows for
user-defined partitions to be added in the future. Since data is sorted by the entity id, the partitioning allows for efficient retrieval of all datoms of a certain type, and indexes will naturally
group together datoms of the same partition.

### Indexing
There are several formats for indexing that may be interesting in this system. In general uses of this system will always know the `Attribute`
being queried, and will be looking for the most recent datom of asof a certain `Tx`. Thus we can get away with a few simple indexes:

!!info
    In this system, when we say "sorted by" we mean that the datoms are sorted by the first element, then the second, then the third, etc, and the sorting is *not* in binary order, but
in logical order. Meaning that 0.0f would come before 1.0f, and "a" would come before "b". This is important because it may be tempting to write a float as a binary blob and sort by that, but
that would result in a chaotic and unpredictable ordering of floats, and perhaps some forms of UTF-8 strings. Since all sorting is done in-memory by C# we can leverage .NET's sorting to maintain
a logical order.

- `EAVT` - This is the most basic index. It sorts the datoms by `Entity`, `Attribute`, and then `Value`. This allows for a simple O(log n) search for the most recent datom
via a simple binary search. The `T` is only used as a tag, as explained below in the section on `index merging`
- `AEVT` - This is a secondary index that sorts the datoms by `Attribute`, `Entity`, `Value`, and `Tx`. This can be used to quickly find all entities that have a certain attribute, and optionally
a specific value.
- `AVET` - This is a secondary index that sorts the datoms by `Attribute`, `Value`, `Entity`, and `Tx`. This allows for a simple O(log n) search for all datoms based on a certain `Attribute`
  and optionally value `Value`. This would allow for a search of all datoms of a certain type that have a value in a given range. Because values may bloat the index, this index is not used by default,
and must be enabled specifically by an attribute placed on the attribute's entity.
- `VAET` - This is a secondary index that sorts the datoms by `Value`, `Attribute`, `Entity`, and `Tx`. This is known as the `reverse index` and is only enabled for indexed attributes (as in the `AVET` index)
or for attributes that have are of the type `Reference`. This allows for a quick request of all entities that reference a given entity. If a mod points to a lodout, this index would allow for a quick
query of all mods that point to a given loadout.

- `Log` - Not technically a index, this is just a linear log of every transaction in the system, sorted by `Tx`. This allows for a simple playback of all transactions in the system and is used as the final source
of truth in the system. This log is append-only.


### Index Merging
A nieve implementation of the above indexes would result in a large amount of redundant data. For example, the `EAVT` index would contain the `Entity` and `Attribute` for every datom over all time. Seeking forward in time would
require skipping over all the old datoms to find the most recent one, this would result in a degrading of performance over time. As it turns out, most users only ever care about the most recent few transations. To solve this problem,
internal indexes consist of three parts:

- `Current` - the current index consists of all the valid datoms in the system. If a datom is updated or changed this index is updted to reflect the most recent datom and the old datom is removed. This means however that the
current index may be missing some data or some data may be incorrect if a query is for a `Tx` that is not the most recent. In order to solve this problem, the `Current` index contains a `Tx` tag for every datom, if the querying
`asof` `Tx` value is less than the value for a given datom, that datom's value is discarded from the query and the value is looked up in the historical index.
- `Historical` - the historical index contains every datom that has ever been in the system, and every datom that has ever been removed. The performance of this index is not as important as the `Current` index, as it will only ever
be queried if the `asof` `Tx` value of the query filteres out values in the `Current` index. This index is append-only and data is never directly deleted from it.
- `In-Memory` - new datoms are first flushed to the log, then added to a in-memory index. This index can grow to some arbitary size before it is merged into the current and history indexes. Since this index is in-memory, it is
vunerable to data loss in the event of a crash, but the In-Memory index can be easily rebuilt by querying the `Log` on startup.

A read of a index then consists of the following flow:

* If the `asof` `Tx` is less than or equal to the Current `Tx` value, the `In-Memory` index is not considered
* Query the `In-Memory` index for a datom, if the datom is found, and the T value is less than the `asof` `Tx` value, discard the datom and look at the `Current` index
* If the datom in the `Current` index is greater than the `asof` `Tx` value, discard the datom and look at the `Historical` index
* Return the most recent datom from the `Historical` index that is less than or equal to the `asof` `Tx` value

This in essence results in a 3-way merge of the `In-Memory`, `Current`, and `Historical` indexes.

### Index Storage

Indexes are stored in a Mix-Max index format as a set of blocks. Datoms are organized into blocks with the following binary format:

```
1 byte - Block version/type
2 bytes - Block size in Datoms, max of 65535 datoms per block (likely less)
[Block size] - E values as ulongs
[Block size] - A values as ulongs
[Block size] - V values as blobs
[Block size] - T values as ulongs
[Block size in bits] - Op values as bits, 1 for add, 0 for remove
[Data Blob] - A blob of data that is referenced by the V values
```

The values are stored as a byte prefixed list of 64bit ulongs. This allows for the most common data types to be inlined into the block itself, while larger data types are stored as blobs.
In the case of values over 8 bytes, the values is stored as a blob in the `Data Blob` section of the block, and the inlined value becomes a offset in that blob.

All this taken into consideration, the minimal size of of a 1024 datom block is:

- 1 byte - Block version/type
- 2 bytes - Block size in Datoms
- 4 * 1024 * 8 bytes - Columns for E, A, V, and T
- 1024 bits - Op values

Totalling 32899 bytes, or roughly 32KB. This block is then likely compressed by the storage layer.

### Index Structure
Since the amount of data involved in this system is likely not going to reach into the billions of datoms, we can suffice with a two-tiered index structure.
the lowest level is blocks in the above format, these blocks are then placed into a list of blocks that record the min and max values of the block, and the GUID of the block. This top level block
is then stored in the storage layer, and it's GUID is written as the most recent `root` for the given index. Since this top level root
is so small, it will likely always exist in-memory. The structure of this top level root follows the same format as the other blocks:

```
1 byte - Block version/type
4 bytes - Block size in blocks, max of 4billion blocks per index (likely less)
[Block size] - E values as ulongs
[Block size] - A values as ulongs
[Block size] - V values as blobs
[Block size] - T values as ulongs
[Block GUIDs] - GUIDs of the blocks
[Datablob] - A blob of data that is referenced by the V values
```

The memory cost for one block is then roughly:
- 16 bytes - E values (min/max)
- 16 bytes - A values (min/max)
- 16 bytes - V values (min/max)
- 16 bytes - T values (min/max)
- 16 bytes - GUIDs of the block
= 80 bytes per block

If we assume a max block size of 4096 datoms, and 1 billion datoms in the entire system, that results in a top level block of 16MB which is easily held in memory.

Naturally, updating the top level block is a rare operation and only happens during an index merge, so transactions themselves can happen quickly (writing only to the log) and the index merge can happen in the background.

When data is added to a block, the block may reach a size where it is no longer small enough to be stored in a single block. In this case the block is split into two blocks, and the root is updated to reflect these changes.

### Garbage Collection
Over time, old blocks are left orphaned as the system updates and changes. These blocks are occasionally GC'd by the system. The GC process is simple, and consists of the following steps:

* Make sure no part of the process is using a old version of the `Current` index
* Walk every index and find all the GUIDs that are referenced by the active indexes
* Walk the storage layer and delete all the blocks that are not referenced by the active indexes
