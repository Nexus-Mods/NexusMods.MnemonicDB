---
hide:
  - toc
---

## Serialization

Events and snapshot data are stored in a custom, extremely high performance binary serialization format. Since events are
readonly, and snapshots are versioned, there is a lot of flexibility in the serialization format.

The core of the serialization format is the `ISerializer` interface. This interface is indicating if a given serializer
(registered via Dependency Injection) can serialize a given type. If it can, the `TryGetFixedSize` may be called to get
the size of the serialized data. This is used to pre-allocate buffers for serialization. Returning false from this method
means that the serializer does not support fixed size serialization, and dynamic sized serialization will be used.

Serializers then implement either `IFixedSizeSerializer` or `IVariableSizedSerializer`. The fixed size serializer is used
when the `TryGetFixedSize` method returns true. The dynamic size serializer is used when the `TryGetFixedSize` method returns
false.

`IEvent` records are seralized via the BinaryEventSerializer. This serializer is designed to be extremely fast, and auto
generates the serialization code for each event type. The format isn't documented, but in general the members are sorted
by name, and the fixed sized members are written first in a large block. This allows for the fixed sized members to be
pre-allocated and serialized in a single block. Since all events are marked with an `EventId` attribute, no type information
is included in the serialized data.

!!!info
    Events are assumed to be immutable, records and are not versioned. This is a solid assumption based on the structure of
    the system, and allows for the serialization format to be extremely fast. But this does mean that special consideration
    must be taken when designing events. Prefer to use a new event type for each change, rather than assuming that a new
    property will be added to an existing event.

## Composite Types
Currently, composite types are not supported. This includes tuples and classes. This functionality could be added in the future,
but for now it's easy enough to structure records to avoid the need for composite types.

For example, let's say an event needs to add 3 files, instead of writing an event like this:

```csharp
public record AddFiles(string name, (string path, string hash)[] files) : IEvent;
```

Restructure the event to use several value arrays:

```csharp
public record AddFiles(string name, string[] paths, string[] hashes) : IEvent;
```

Not only does this avoid the need for composite types, but it also likely is slightly faster to serialize and deserialize,
as all the values of like type are packed together
