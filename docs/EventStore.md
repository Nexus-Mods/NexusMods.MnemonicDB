---
hide:
  - toc
---

## Event Store
The Event Store is a database that allows for efficient storage and retrieval of events. It is a key component of the Event Sourcing pattern. The primary
methods on `IEventStore` allow for adding an event to the store, and retrieving all events for a given entity over a given range of transaction values.

### Terms
* **Event**: An event is a record of something that happened in the system. It is a record of a fact, not a command to do something. Events are immutable. If an event is recorded in error, a new event
    should be recorded to correct the error. If the structure of an event must radically change, a new event of a new type should be recorded. Events are identified by a unique identifier, which is a guid
     (stored as a UInt128).
* **Entity**: An entity is a thing in the system that has a unique identity. Entities are identified by a unique identifier, which is a guid (stored as a UInt128). Entities are mutable, and only
    change as a result of events. Entity state for a given transaction *can* change however, if the definition of the entity or events change. To support this, entities are assigned a unique entity
    type identifier (a guid, stored as a UInt128), and a revision number (a uint16). The combination of the entity type identifier and revision number uniquely identify the definition of an entity.
* **Transaction**: A transaction is a collection of events that are stored together. Transactions are identified by a sequential (uint64) number, starting at 1.
* **Snapshot**: A snapshot is a record of the state of an entity at a given transaction. Snapshots are immutable, but can be replaced by a newer snapshot if an entity definition changes
