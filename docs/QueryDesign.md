---
hide:
  - toc
---

## Query Design

The architecture of MnemonicDB is fairly simple: tuples are stored in indexes sorted in various ways, the logging function
(in `IDatomStore`) publishes a list of new databases and from those the updates in each transaction can be determined. This
simple format allows for a wide variety of queries to be performed on the data and optimally a lot of performance can be gained
in many parts of query.

### Goals
A few goals of what is desired in the query design:

* **Performance**: The queries should not require O(n*m) operations as much as possible, while parts may be implemented
simply and have higher complexity, options should be left in the design for optimization.
* **Larger than memory**: The query results are expected to fit in memory, but the source datasets may not. This means that
as much as possible only the minimal working set should be loaded into memory.
* **System Sympathy**: The queries should be designed to work well the rest of the database, indexes store data pre-sorted,
queries should be designed to take advantage of this.
* **Live Queries**: C#'s DynamicData is a fantastic library for UI programming and is close (but not quite) to something
that can be used by MnemonicDB. The queries should be designed to work well with this library, but also provide some sort
of delta-update systems such as `IObservable<IChangeSet<T>>` or similar. This allows for small transactions to not require
the entire query to be re-run. A delta update system fits very well with MnemonicDB's transactional publish queue, as each
transaction can result in a delta of datoms added (or removed) from the database.


### Concepts

* **IConnection**: The connection is the primary interface for talking to the database, it can be "dereferenced" by calling
`conn.Db` to get a immutable database. This interfaces also provides an `IObservable<IDb>` for subscribing to updates to the
database. It also provides a `IObservable<IDb, IndexSlice>` for subscribing to updates along with the portion of the `TxLog`
added in each transaction.
* **IDb**: The database is a readonly view of all the data in the database as of a specific point in time.
* **SliceDescriptor**: A description of a slice of an index in the database. It defines how a database value should be
interpreted and which index to use. This can be thought of as a "view" on the database, consisting of a tuple of `[index, from, to]`.
SliceDescriptors are immutable, and are compared by value. This means that they can be used as keys in dictionaries and sets, and makes
them useful for caching.
* **ObservableSlice**: A slice that can be subscribed to for updates. This is a `IObservable<IChangeSet<Datom>>` and is constructed
by combining a `SliceDescriptor` and a `IConnection`.
* **IndexSlice**: A loaded chunk from the database, made by combining a `SliceDescriptor` and a `IDb`.

### Query Design

Based on these simple primitives, a wide variety of queries can be constructed, two IndexSlices can be joined to filter each other,
the datoms in the index can be grouped, sorted, and filtered in various ways. The `ObservableSlice` can be used to create live queries
of the database.

#### Example Queries

