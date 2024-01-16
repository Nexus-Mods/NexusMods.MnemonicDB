---
hide:
  - toc
---

## Secondary Index
By default every entity in the system in indexed by the Entity ID, retrieving an entity by its ID is always a very fast operation.
However, there are times when you want to retrieve entities by other properties. For example, you may want to retrieve all the entities that have a given name,
or all the entities that have a given type. These operations benefit from having an index on the property you are searching for. In this case you
would need to mark an attribute with the "Indexed" attribute and give it a unique Guid. This Guid will be used to identify the index, even when the attribute is renamed,
or moved into a different class in the inheritance hierarchy.

```csharp

    [Indexed("NameIndex")]
    public string Name { get; set; }

```

### Index Structure
In EventStores must support secondary indexes. In RocksDB's case they are structured as follows:

```
[Attribute, Value, TXId]
```

The Attribute is the U128 representation of the Guid that was used to mark the attribute with the Indexed attribute. The Value is the value of the attribute, and the TXId
is the transaction that the value was set in. Thus finding all the entities with a given value is a simple matter of scanning the index for the given attribute and value,
and ignoring any entries that have a TXId greater than the transaction you are interested in. This is abstracted away from most of the system via the `IEventStore` interface.

```
void GetEntitiesForAttributeValue<TAttribute, TValue>(TValue value);
```
