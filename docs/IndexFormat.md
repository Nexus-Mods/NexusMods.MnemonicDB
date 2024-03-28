---
hide:
  - toc
---

## Index Format

The index format of the framework will look familiar to those who have used similar databases like Datomic or Datahike.
Since MneumonicDB is based on RocksDB, most of the complexity of handling the index is abstracted away. Values are stored
in several "Column Families" which are RocksDB's way of storing data in separate files or partitions. The tuples are stored
exclusively in the key of the store, and the value is always empty. All the indexes store exactly the same format of key
layouts, and only the sorting logic of each index is different.

### Key Format

Keys are stored as a 16byte `KeyPrefix` followed by an arbitrarily sized value blob. Since RocksDB tracks the length of
the keys, there is no need to store the length of the value in the key. The `KeyPrefix` is a bitpacked value that contains
the following information:

```
   [AttributeId: 2 bytes]
   [TxId: 6 bytes]
   [Entity Partition Id : 1 byte]
   [EntityID : 6 bytes]
   [IsRetract: 1byte]
```

The side effect of this bit packing is that the database can ever only have 64K attribute definitions, and roughly 180
quintillion entities and transactions, these are considered to be reasonable limits for the database.

### Indexes
There are 5 categories of indexes that are maintained in the database:

* TxLog : this contains a sorted set of all data in the database sorted by transaction id. This allows for queries to
replay the database as of a specific transaction id, or to ask "what changed in this transaction".
* EAVT : this contains all datoms sorted primarily by Entity, then Attribute, then Value, then Transaction. This is most
commonly used for queries that ask "what is the value of this attribute for this entity", and is used to load an entity.
* AEVT : this contains all datoms sorted primarily by Attribute, then Entity, then Value, then Transaction. This is most
commonly used to look up all entities that have a specific attribute value. For example, querying for all the users in
the database.
* VAET : this contains datoms where the value is an EntityID also known as a reference. This index is used to look up
all the references to a specific entity. For example, querying for all the comments on a blog post.
* AVET : this contains datoms that have `isIndexed` set to true, and is the "reverse index". This is used to look up all
entities that have a specific attribute. For example, querying for all the users with a given email address.

Since MnemonicDB is a temporal database, the last four indexes are maintained in a `current` index. When a value is
overwritten, it is evicted (deleted) from the `current` index and added to the historical index. This allows for fast
queries for the most recent data, and slower queries for historical data (as they must scan over all past values).

When a transaction is committed, the system creates a snapshot of the underlying storage. This snapshot is then used to
keep the state of the database live as long as any part of the application has a reference to it. After the snapshot is
reclaimed, future queries for that specific TxId will need to use the historical indexes to find the data. While this sounds
like a serious tradeoff, chances are that an application will dereference the database, use a snapshot, then go back and
get an updated copy of the database, releasing the old snapshot. Going backwards in time is a rare operation, but supported.

Attributes can be labeled as `noHistory`, in which case, when they are evicted from the `current` index, they are simply
discarded. This is useful for attributes that are not important to keep a history of, but have a high rate of change, such
as a "last seen" timestamp.

### Why is history slower?

The historical indexes are slower because they contain stale data. When queried, the historical indexes must scan over all
past values for a given entity or attribute pair, and select the maximum value. This then means that the complexity of these
operations are O(n) where n is the number of values for a given entity or attribute pair. The current indexes are O(1) as they
only need to look up the value in the index. In practice, loading datoms is extremely fast, and the overhead of scanning the
historical indexes is low, but it is a consideration when designing an application.

### Making changes to the schema

New attributes can be added to the database at any time, and the database will automatically start indexing them. In addition,
old attributes need not remain in the C# codebase, MneumonicDB will simply skip over them when loading values. So as much
as possible, try to make additive changes to the schema, and avoid changing attributes. Attributes are named after the classes
by convention, but this is not a requirement, and the database will work just fine if you change the class name of an attribute,
as long as the attribute's unique ID remains the same. Thus deprecated attributes can be moved to a `Deprecated` namespace, and
left to sit.

### Migrations

Migrations are not yet implemented, but the idea is fairly simple, a new database is created, and the TxLog of the source
is replayed into the target with some sort of transformation process happening on the way. This is a future feature, and
planned to be implemented soon.












