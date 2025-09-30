## Changelog

### 0.27.3 - 2025-09-30
* Fix reading `TimestampNs` values

### 0.27.2 - 2025-09-24
* Fix a bug datoms table initialization

### 0.27.1 - 2025-09-24
* Fix a bug with table registration

### 0.27.0 - 2025-09-24
* Fix several bugs
* Allow injection of functions via DI
* Upgrade to DuckDB 1.4

### 0.26.1 - 2025-09-24
* Fix ObserveDatoms bug with adds and subtracts in the same transaction on the same attribute

### 0.26.0 - 2025-09-22
* Expose MDB timestamps as DuckDB timestamps
* SourceCache adaptors now better track changes to specific rows and values

### 0.25.1 - 2025-09-18
* Make unconstrained calls to mdb_Datoms total-ordered

### 0.25.0 - 2025-09-13
* Add support for MDB bool types as DuckDB bool types
* Fix for unmanaged types not propagating as nulls in DuckDB

### 0.24.0 - 2025-09-13
* Add support for list value results

### 0.23.2 - 2025-09-06
* Add adaptors for 5-8 element tuples

### 0.23.1 - 2025-09-06
* Add bindings for getting child values from a struct logical type in HyperDuck

### 0.23.0 - 2025-09-04
* Fix namespacing issue with ambient queries
* Simplify type conversion for binding and value adaptors
* Add support for interpolated queries

### 0.22.1 - 2025-08-27
* Add a prefix parameter to Connection

### 0.22.0 - 2025-08-27
* Add support for .sql query files, that will be loaded into the DuckDB engine at startup

### 0.21.0 - 2025-08-13
* Implement support for querying cardinality many attributes with DuckDB
* Support for nullable types returned from DuckDB queries
* Added support for named parameters in Query

### 0.20.0 - 2025-08-12
* Added binding converters and value adaptors for all the main primitive types (including 128 ints)

### 0.19.5 - 2025-08-12
* Fixed several shutdown errors by better handling "wait on same thread" async issues

### 0.19.4 - 2025-08-07
* Use CancelAfter to solve a deadlock issue on shutdown

### 0.19.3 - 2025-08-06
* Allow returning datetime values from queries

### 0.19.2 - 2025-08-06
* Use .NET's built-in DLL path resolver vs hardcoded relative paths

### 0.19.1 - 2025-08-05
* Fixes for build, include external DuckDB native deps

### 0.19.0 - 2025-08-05
* Switch to DuckDB as the query engine
* Cascade code is removed and replaced with DuckDB
* WARNING: this code is still in testing, should not be used in production until tested a bit more

### 0.18.0 - 2025-06-24
* Add `HasAttribute` and `MissingAttribute` operators for matching entities based on the presence or absence of attributes

### 0.15.0 - 2025-06-04
* Add user defined defaults for `MatchDefault`
* Rework the `LatestTxForEntity` node to properly handle deleted entities

### 0.14.0 - 2025-05-28
* Transactions can now be nested
* Support for nullable `Null` values, something that was needed for the `Cascade` query system
* Add overload for `DbOrDefault` that uses the default db

### 0.13.0 - 2025-05-15
* Tweak how Connection and DatomStore are shutdown to avoid deadlocks

### 0.12.0 - 2025-05-08
* Upgrade to the latest version of Cascade and allow for any version higher than the current

### 0.9.123 - 2025-05-05

* Improve performance when iterating over datoms in `EntitySegment`
* Massively improved the initial seek performance of the RocksDB code. The backend now leverages bloom filters (on new blocks)
to reduce the number of datom comparisons by a factor of 10-1000x
* Massively improved iteration and lookup performance of the query routines
* Optimized the most heavily used paths of the `GlobalCompare` code resulting in a 2x speedup on compare bound operations
* Added Cascade query support
* Backwards compatible with existing uses of the database

### 0.9.122 - 2025-03-26

* Upgrade `NexusMods.Paths` to 0.18.0

### 0.9.121 - 3/13/2025
* Optimize many of the `.Datoms` calls
* Performance improvements of Backref lookups and indexes

### 0.9.120 - 3/12/2025
* Fix a bug in the generic slice descriptor that didn't allow for it to query historical indexes

### 0.9.119 - 3/11/2025
* Fix a bug with AVSegments and long values
* Remove an unused parameter from the `IAttribute.TryGetValue` method

### 0.9.117 - 3/11/2025
* Fix a bug only found in NMA that errored because the initial Db didn't have its connection set

### 0.9.116 - 3/11/2025
* Fix a bug with Analyzers not being run the first time an existing database is opened

### 0.9.115 - 3/11/2025
* Many internal optimizations, should not affect the public API

### 0.9.114 - 3/3/2025
* Fixed a compile error in the build pipeline

### 0.9.113 - 3/3/2025
* Removed "DelayUntilFirstValue". It's no longer needed and was causing a deadlock in some cases

### 0.9.112 - 26/2/2025
* Drastic improvements to the performance of the `Datoms` calls. Now these calls bottom out on raw RocksDB iterators and structs
* Implements a `db.PrecacheAll()` method that will pre-cache all entities in the `Entity` partition. As well as any reverse indexes, useful for
loading data in an app that will very quickly touch the entire active set of entities. This load operation is done in a single iteration, so the performance
is at least one or two orders of magnitude faster than loading via lazy traversal.
* Added new struct based versions of `SegmentDefinition`, that only contain the variable data about the segment being loaded. This results
in less memory allocation and generally faster performance.
* The InMemory backend is now gone, all databases are based off of RocksDB, and in-memory databases are created by using RocksDB's in-memory environment.

### 0.9.111 - 24/2/2025
* Set the connection event thread to "IsBackground" so that it doesn't prevent the application from shutting down

### 0.9.110 - 24/2/2025
* Properly shut down the event queue in the connection when the connection is disposed.

### 0.9.109 - 24/2/2025
* Switch observers over to running purely on a thread responsible for handling events in the connection. This ensures that we never
lose any data in observables, but data will arrive slightly after a subscription is created. Hopefully this works. 

### 0.9.108 - 24/2/2025
* Completely switch over to lock free algorithms for `ObserveDatoms`. To implement this a new (internal) immutable interval tree was created
and CAS operations are leveraged to maintain consistency. Should result in a performance improvement in some cases and a reduction in lock contention. 
  * Datom observables check for DB updates before sending their initial changesets, but it is possible under heavy load to miss an update. It is yet
to be seen if this will result in issues in practice. 

### 0.9.107 - 19/2/2025
* Revert some of the `ObserveDatom` changes to respect the previous behavior, but keep other optimizations and proper disposal handling of observables

### 0.9.106 - 17/2/2025
* Switch to a custom build of the interval tree that supports `O(n)` removals when bulk removing. Previous method was `O(m * n)`.

### 0.9.105 - 17/2/2025
* Reworks the internals of datom observers so that they are mostly lock-free. This should result in a significant performance
and yet still not deadlock on nested observables

### 0.9.104 - 17/2/2025
* Fix a bug with the below-mentioned Unique attribute feature when used with an existing database. The new constraint is now
backwards compatible.

### 0.9.103 - 12/2/2025
* Placeholder release to fix issues in the build pipeline

### 0.9.102 - 12/2/2025
* Add support for Unqiue constraints on attributes. Setting `.Unique = true` on an attribute will cause the database to ensure
that no two entities have the same value for that attribute. This is enforced at the database level and will cause a transaction
to fail if the constraint is violated
* Fix a deadlock issue when nested obervables are constructed inside the callback chain of a `Connection.ObserveDatoms` update

### 0.9.101 - 28/1/2025
* Add a option to Connection that allows the connection to be started in read-only mode (not running simple migrations)

### 0.9.100 - 27/1/2025
* Switch timestamps over to storing time as `DateTimeOffset.Ticks` so that we have the most accurate times possible
* Provide a new `ScanUpdate` feature that can be used for data migration and conversions. Allows datoms to be deleted or 
updated based on a simple function interface

### 0.9.99 - 21/1/2025
* Provide a way to open a RocksDB backend in read-only mode
* Default to zstd compression for RocksDB vs the previous snappy compression
* Provide a `FlushAndCompact` method on the connection so that the user can manually trigger a compaction of the database

### 0.9.98 - 16/1/2025
* Massively improve performance of the `ObserveDatoms` function. It is now ~200x faster than the previous version
* Clean up the logging in the inner transacting loop, switch to high performance logging for those few critical messages

### 0.9.97 - 14/11/2024
* Fix a nuget packing bug with the source generators

### 0.9.96 - 14/11/2024
* Update to .NET 9.0
* Moved a lot of attributes from NexusMods.App into this codebase
* Added attributes for a lot of primitive types. Int32, UInt64, etc.
* Removed a lot of warnings from the library and source generated code

### 0.9.95 - 30/10/2024
* Fix a regression with the switch to DateTimeOffset, we now store the correct timestamp in transaction attributes
* 
### 0.9.94 - 30/10/2024
* Fix a regression in the GlobalCompare that was boxing an enum (resulting in a lot of allocations)

### 0.9.91 - 15/10/2024
* Attempt to fix an issue with Nuget and package resolution

### 0.9.90 - 15/10/2024
* Reworked serialization of Tuple attributes. The result more efficient (in space and time) but limited in the number and types of elements that can be stored in a Tuple.
* Reworked the internals of the DatomStore to support more types of specialized transactions such as Excision
* Implemented a basic form of schema migration.
 * Indexes can be added and removed
 * Attributes can be added and removed (removed attributes are ignored)
 * Attribute types can be changed (in a very limited fashion, to be expanded in the future)

### 0.9.89 - 09/10/2024
* Fixed a bug with case-insensitive string comparisons in the database. This would cause an "Invalid UTF-8" exception to be thrown

### 0.9.88 - 09/10/2024
* Added support for historical databases. These are instances of `IDb` that contain all datoms, inserted, retracted, and historical.
Can be useful for analytics or viewing the changes of an entity over time
* Added excision support to the database that will allow for complete removal of datoms, including historical datoms.

### 0.9.87 - 08/10/2024
* Added import/export functionality for the database.

### 0.9.86 - 01/10/2024
* Swapped out `R3`'s behavior subject for a custom implementation that is lock-free
* Reworked how updates are propagated through the system. Thanks to the nature of TxIds we can detect gaps in the sequence of updates
and fill in missing results with `AsOf` queries. This allows us to remove the locks used during subscription and greatly reduce the
possibility of deadlocks.

### 0.9.84 - 20/09/2024
* Fixed a bug with Tuple3 values that had a reference in the first position.
* Added a user accessible remap function for values

### 0.9.83 - 20/09/2024
* Optimized the interface with RocksDB used all throughout the library. Results in a 30% speedup on search operations
inside RocksDB.
* Removed the IAttributeRegistry and all associated IDs and implementations. Functionality is now split into two sub-modules
  * AttributeCache - sealed class that is created by the backend store and is purely based on the database schema, used by most of the system
  * AttributeResolver - DI dependant code that the frontend of the system uses to resolve DB datoms to C# attributes. Used very rarely
* It is now possible for most DB operations to be run completely ignorant of the C# attribute definitions.  

### 0.9.82 - 12/09/2024
* Fix a O(n) issue caused by Rx storing observers in a ImmutableList inside a `BehaviorSubject`. Switched to using R3 internally. Over 
time Rx's uses will be replaced with R3 to avoid these and several other issues

### 0.9.81 - 9/09/2024
* Fix a bug the source generators when trying to use HashedBlobAttributes

### 0.9.80 - 22/08/2024
* `IAnalyzer` gets both the new database and the old database as arguments. This allows for looking up the previous state of entities for retractions

### 0.9.79 - 19/08/2024
* Fix a bug with `Tuple<T1, T2, T3>` attributes when the first member is a reference type. This caused temporary IDs to not
be resolved correctly when the temporary ID was resolved

### 0.9.78 - 15/08/2024
* `Model.ObserveAll` now returns `IObservable<IChangeSet<ReadOnly, EntityId>>` instead of `IObservable<IChangeSet<ReadOnly, DatomKey>>` which makes it
easier to use in projects without polluting the code with `DatomKey` references. `Connection.ObserveDatoms` still returns `IObservable<IChangeSet<Datom, DatomKey>>`

### 0.9.77 - 15/08/2024
* Added extension methods to query multiple indexed datoms at once. Results from the database are merge-joined into a
single list that is extremely fast to calculate. 
* Rewrote `ObserveDatoms` resulting in a roughly 1000x speedup when 10,000 datoms are being observed. The function now returns
`IObservable<IChangeSet<Datom, DatomKey>>` which allows for extremely efficient set-like operations on the datoms being observed
