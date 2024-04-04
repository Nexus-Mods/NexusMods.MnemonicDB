---
hide:
  - toc
---

## Index Format

The index format of the framework will look familiar to those who have used similar databases like Datomic or Datahike.
Since MnemonicDB is based on RocksDB, most of the complexity of handling the index is abstracted away. Values are stored
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

### Indexing Strategy

As mentioned the 4 main indexs are partitioned into two column families the `Current` and the `History`, indexes. These
sub-indexes share the same sorting (and comparator) logic, but are stored in separate files. In addition the `Current` index
never contains any `Retraction = true` datoms. This is because a retraction is a logical deletion of a value, and any deleted
values are evicted from the `Current` index and added to the `History` index. This allows for fast queries for the most recent
data, and slower queries for historical data (as they must replay the history).

A interesting design choice made in the original Datomic implementations was to sort the indexes by `T` after other values,
this is a subtle choice that was lost on the initial implementor of MnemonicDB. This is best illustrated with an example:

Let's look at a subsegment of the EAVT indexes for a single entity:

R  | E                | A            | V                  | T                |
-- |------------------|--------------|--------------------|------------------|
+ | 0200000000000001 | (0014) Path  | /foo/bar           | 0100000000000002 |
+ | 0200000000000001 | (0015) Hash  | 0x00000000DEADBEEF | 0100000000000002 |
+ | 0200000000000001 | (0016) Size  | 42 B               | 0100000000000002
+ | 0200000000000001 | (0017) ModId | 0200000000000003   | 0100000000000002
- | 0200000000000001 | (0017) ModId | 0200000000000003   | 0100000000000003
+ | 0200000000000001 | (0017) ModId | 0200000000000005   | 0100000000000003


!!! info "Printing IDs in hex format is helpful as it clearly shows the partition value of the IDs. Without this we would
    see arbitrary base 10 numbers that would be hard to interpret."

We can see that these datoms are sorted by `E`, `A`, `V`, and then `T`. The `+` and `-` symbols indicate if the datom is
an assertion (`+`) or a retraction (`-`). The `T` value is the transaction id, and we can see that the `ModId` attribute
was changed in transaction `0100000000000003` to point to a different entity.

If we wanted to read this entity into a C# object like a hashmap, we could read each datom in order. If an attribute is
a `+` we would add it to the map, and a `-` would delete it from the map. But we have a very clear optimization available
to us because all the tuples are sorted by `E` and then `T`. We can simply read the datoms in order, and if we see a `+`
we wait to add it to the map until we read the next datom. If the next datom is a `-` we skip both datoms. Otherwise we add
the previous datom to the map, and save the current datom as the "current" value. This means that with a very small amount
of scratch space we can easily replay the entire entity state from the database.

The second thing that the author of MnemonicDB missed (that was included in Datomic) was that this optimization *only*
works if scalar attributes (attributes with a single value, not sets of values) must record an implicit retraction when
they are updated. This terminates the previous value and asserts a new value. Performing this optimization means more
data must be recorded to disk, but allows the read logic to be *extremely* simple. Infact this logic can be implemented
inside an iterator in just a few lines of code.

### Historical Queries

While querying the Current index is fast, occasionally users will want to query the database as of an arbitrary `T`value.
This is known as a `Historical`, or `Temporal` query. When this operation needs to be performed the general algorithm is
rather simple:

* Given a `Start` and `End` datom that mark the range of the query
* Get the iterator for the `Current` index for the given range, and the `History` index for the given range
* Merge the iterators together via a `Sorted Merge`
* Filter the merged iterator remove any datoms that are newer than the `AsOfTx`
* Apply the above datom filtering to the iterator.

The relative simplicity of this operation can be seen in the following C# code:

```csharp
    public IEnumerable<Datom> Datoms(IndexType type, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var current = inner.Datoms(type.CurrentVariant(), a, b);
        var history = inner.Datoms(type.HistoryVariant(), a, b);
        var comparatorFn = type.GetComparator(registry);
        var merged = current.Merge(history,
            (dCurrent, dHistory) => comparatorFn.Compare(dCurrent.RawSpan, dHistory.RawSpan));
        var filtered = merged.Where(d => d.T <= asOfTxId);

        var withoutRetracts = ApplyRetracts(filtered);
        return withoutRetracts;
    }
```

!!! info "A sorted merge is a common operation in many database systems. It involves walking two datasets (iterators in
         our case) and comparing the current value of each iterator. The smaller value is emitted, and that iterator is
         advanced. This is repeated until both iterators are exhausted. Since both inputs are sorted by the same comparator,
         no complex sort is required and the complexity of this operation is `O(n + m)` where `n` and `m` are the number
         of items in the two iterators."

### Multi cardinality attributes
When an attribute is marked as being multi-valued (or known as cardinality many) the database will store multiple values
for the same attribute. The logic for this behavior is rather simple, we simply don't create a retraction for datoms when
we write a new one. This means that datoms continue to live until they are explicitly retracted. Multi-valued attributes
are useful for operations like tags or many-to-many relationships where the values are more than a simple link to some
other entity.

## Long Example

Let's put this all together into an example. Let's say the state of our two indexes is as follows:

#### EAVT Current
 R  | E                | A                             | V                                                | T
----|------------------|-------------------------------|--------------------------------------------------|-------------------
 +  | 0000000000000001 | (0001) UniqueId                 | NexusMods.MnemonicDB.DatomStore/UniqueId         | 0100000000000001
 +  | 0000000000000001 | (0002) ValueSerializerId        | NexusMods.MnemonicDB.S...izers/SymbolSerializer  | 0100000000000001
 +  | 0000000000000002 | (0001) UniqueId                 | NexusMods.MnemonicDB.D...tore/ValueSerializerId  | 0100000000000001
 +  | 0000000000000002 | (0002) ValueSerializerId        | NexusMods.MnemonicDB.S...izers/SymbolSerializer  | 0100000000000001
 +  | 0200000000000001 | (0014) Path                     | /foo/bar                                         | 0100000000000002
 +  | 0200000000000001 | (0015) Hash                     | 0x00000000DEADBEEF                               | 0100000000000002
 +  | 0200000000000001 | (0016) Size                     | 42 B                                             | 0100000000000002
 +  | 0200000000000001 | (0017) ModId                    | 0200000000000005                                 | 0100000000000003
 +  | 0200000000000002 | (0014) Path                     | /foo/qux                                         | 0100000000000003
 +  | 0200000000000002 | (0015) Hash                     | 0x00000000DEADBEAF                               | 0100000000000002
 +  | 0200000000000002 | (0016) Size                     | 77 B                                             | 0100000000000002
 +  | 0200000000000002 | (0017) ModId                    | 0200000000000003                                 | 0100000000000002
 +  | 0200000000000003 | (0018) Name                     | Test Mod 1                                       | 0100000000000002
 +  | 0200000000000003 | (0019) LoadoutId                | 0200000000000004                                 | 0100000000000002
 +  | 0200000000000004 | (001A) Name                     | Test Loadout 1                                   | 0100000000000002
 +  | 0200000000000005 | (0018) Name                     | Test Mod 2                                       | 0100000000000002
 +  | 0200000000000005 | (0019) LoadoutId                | 0200000000000004                                 | 0100000000000002
 +  | 0200000000000006 | (001B) Name                     | Test Collection 1                                | 0100000000000002
 +  | 0200000000000006 | (001C) LoadoutId                | 0200000000000004                                 | 0100000000000002
 +  | 0200000000000006 | (001D) Mods                     | 0200000000000003                                 | 0100000000000002

#### EAVT History
 R  | E                | A                             | V                                                | T
----|------------------|-------------------------------|--------------------------------------------------|-------------------
+ | 0200000000000001 | (0017) ModId                    | 0200000000000003                                 | 0100000000000002
- | 0200000000000001 | (0017) ModId                    | 0200000000000003                                 | 0100000000000003
+ | 0200000000000002 | (0014) Path                     | /qix/bar                                         | 0100000000000002
- | 0200000000000002 | (0014) Path                     | /qix/bar                                         | 0100000000000003
+ | 0200000000000006 | (001D) Mods                     | 0200000000000005                                 | 0100000000000002
- | 0200000000000006 | (001D) Mods                     | 0200000000000005                                 | 0100000000000003

### Step 1 - Range Queries
Now let's query the datoms for entity `0200000000000002` as of transaction `0100000000000002`. First we get the datoms
for the `Current` and `History` indexes for the given entity:

#### EAVT Current
 R  | E                | A                             | V                                                | T
----|------------------|-------------------------------|--------------------------------------------------|-------------------
 +  | 0200000000000002 | (0014) Path                     | /foo/qux                                         | 0100000000000003
 +  | 0200000000000002 | (0015) Hash                     | 0x00000000DEADBEAF                               | 0100000000000002
 +  | 0200000000000002 | (0016) Size                     | 77 B                                             | 0100000000000002
 +  | 0200000000000002 | (0017) ModId                    | 0200000000000003                                 | 0100000000000002

#### EAVT History
 R  | E                | A                             | V                                                | T
----|------------------|-------------------------------|--------------------------------------------------|-------------------
+ | 0200000000000002 | (0014) Path                     | /qix/bar                                         | 0100000000000002
- | 0200000000000002 | (0014) Path                     | /qix/bar                                         | 0100000000000003

### Step 2 - Sorted Merge

Now we merge the two iterators together:

 R  | E                | A                             | V                                                | T
----|------------------|-------------------------------|--------------------------------------------------|-------------------
 +  | 0200000000000002 | (0014) Path                     | /foo/qux                                         | 0100000000000003
+   | 0200000000000002 | (0014) Path                     | /qix/bar                                         | 0100000000000002
-   | 0200000000000002 | (0014) Path                     | /qix/bar                                         | 0100000000000003
 +  | 0200000000000002 | (0015) Hash                     | 0x00000000DEADBEAF                               | 0100000000000002
 +  | 0200000000000002 | (0016) Size                     | 77 B                                             | 0100000000000002
 +  | 0200000000000002 | (0017) ModId                    | 0200000000000003                                 | 0100000000000002

### Step 4 - Filter by `AsOfTx >= 0100000000000002`

 R  | E                | A                             | V                                                | T
----|------------------|-------------------------------|--------------------------------------------------|-------------------
 +   | 0200000000000002 | (0014) Path                     | /qix/bar                                         | 0100000000000002
 +  | 0200000000000002 | (0015) Hash                     | 0x00000000DEADBEAF                               | 0100000000000002
 +  | 0200000000000002 | (0016) Size                     | 77 B                                             | 0100000000000002
 +  | 0200000000000002 | (0017) ModId                    | 0200000000000003                                 | 0100000000000002

### Step 5 - Apply Retracts

In this case, we don't have any retractions to apply, so our result is the same as the filtered iterator.

 R  | E                | A                             | V                                                | T
----|------------------|-------------------------------|--------------------------------------------------|-------------------
 +   | 0200000000000002 | (0014) Path                     | /qix/bar                                         | 0100000000000002
 +  | 0200000000000002 | (0015) Hash                     | 0x00000000DEADBEAF                               | 0100000000000002
 +  | 0200000000000002 | (0016) Size                     | 77 B                                             | 0100000000000002
 +  | 0200000000000002 | (0017) ModId                    | 0200000000000003                                 | 0100000000000002

But let's say that we were filtering by `AsOfTx >= 0100000000000003` then we would have a retraction to apply:

 R  | E                | A                             | V                                                | T
----|------------------|-------------------------------|--------------------------------------------------|-------------------
 +  | 0200000000000002 | (0014) Path                     | /foo/qux                                         | 0100000000000003
+   | 0200000000000002 | (0014) Path                     | /qix/bar                                         | 0100000000000002
-   | 0200000000000002 | (0014) Path                     | /qix/bar                                         | 0100000000000003
 +  | 0200000000000002 | (0015) Hash                     | 0x00000000DEADBEAF                               | 0100000000000002
 +  | 0200000000000002 | (0016) Size                     | 77 B                                             | 0100000000000002
 +  | 0200000000000002 | (0017) ModId                    | 0200000000000003                                 | 0100000000000002

Which when we apply filtering, would notice that the `/qix/bar` path was retracted in transaction `0100000000000003`, so
we wouldn't emit that pair of datoms, so our final set of datoms would be:

 R  | E                | A                             | V                                                | T
----|------------------|-------------------------------|--------------------------------------------------|-------------------
 +  | 0200000000000002 | (0014) Path                     | /foo/qux                                         | 0100000000000003
 +  | 0200000000000002 | (0015) Hash                     | 0x00000000DEADBEAF                               | 0100000000000002
 +  | 0200000000000002 | (0016) Size                     | 77 B                                             | 0100000000000002
 +  | 0200000000000002 | (0017) ModId                    | 0200000000000003                                 | 0100000000000002
