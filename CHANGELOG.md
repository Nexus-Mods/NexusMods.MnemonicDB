## Changelog

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
