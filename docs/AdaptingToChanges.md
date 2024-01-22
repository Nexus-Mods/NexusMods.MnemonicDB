---
hide:
  - toc
---

## Adapting to Changes
The EventSourcing framework is designed to be flexible and allow for changes to the data model. However, there are some changes
that are not easily support.

### Event Replaying
A key component to the EventSourcing framework is the ability to replay events. Since the logic for these events is defined
in code, this code can change at any time redefining how the events are processed. The arguments to the event (the event data)
cannot change over time as the datastore is immutable, but the interpretation of that data can change. The attributes emitted
by an event can completely change, and as long as the events are updated to match, the system will continue to work.

### Snapshotting
Entities are routinely snapshotted when read to improve performance. The problem with this approach is that they essentially
"bake in" the logic of the events at the time of the snapshot. This means that if the logic of the events changes, the snapshot
will have to be recreated. Due to this, each snapshot records not only the Entity type id, but also its revision. To invalidate
snapshots of an entity, simply increment the revision number on the entity, this will invalidate all snapshots on the next read of the entity
and the events for that entity will be replayed. In small batches this should not be a performance problem as reading events is
fairly inexpensive. Once the entity is re-read a new snapshot with the new revision will be created.


## Example problems and solutions

### Example 1: Changing an attribute type
Lets say you have a `File` entity that has a `.Size` attribute. You realized that someone set that size to `uint` and now the
system breaks because a file is over 4GB. The event that creates this file `CreateFile` has a `uint` parameter on it. As mentioned
above, event data cannot change, so you cannot modify the `uint` on the event and turn it into a `ulong`.

Instead, first update the `File` entity to have a `ulong` for the `.Size` attribute, and increment the revision number.
Then create a new event `CreateFileV2` that that has a `ulong` parameter for the size. Now go to the definition for `CreateFile`
and modify the `Apply` method to convert the `uint` parameter to a `ulong` when emitting the size.

Now the old events will replay correctly (as the `.Apply` method will convert the `uint` to a `ulong`), and any new events
will emit the `CreateFileV2` event with the correct `ulong` size. Incrementing the revision number on the `File` entity will
cause all snapshots to be invalidated and the new events will be replayed during the next load.

### Example 2: Renaming a Entity
You have an entity named `File`, but need to now call it `ArchiveFile`. All entities have a `Entity` attribute that provides
a unique identifier for the entity. So all you need to do is modify the C# name of the entity, and nothing else needs to change. This
also applies for moving an entity to a different namespace.

