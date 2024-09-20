## Changelog

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
