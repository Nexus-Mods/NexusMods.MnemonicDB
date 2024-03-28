---
hide:
  - toc
---

<div align="center">
	<h1>MneumonicDB a in-process temporal database for .NET</h1>
	<img src="./Nexus/Images/Nexus-Icon.png" width="150" align="center" />
	<br/> <br/>
    <br/>
    <img alt="GitHub Workflow Status" src="https://img.shields.io/github/actions/workflow/status/Nexus-Mods/NexusMods.MneumonicDB/BuildAndTest.yml">
</div>

## About

Built for the NexusMods.App project, MneumonicDB is a tuple oriented, typesafe, in-process temporal database for .NET
applications. It supports a pluggable storage and value model leverages [RocksDB](https://rocksdb.org/) by default.
Many similarities can be seen in this project to [Datomic](https://www.datomic.com/), [Datahike](https://github.com/replikativ/datahike),
and [XTDB](https://xtdb.com/)

by default, but has a pluggable storage layer.

### Definitions
The above description is a bit of a mouthful, so let's break it down a bit.

* **Tuple Oriented**: Data is stored in the database in the format of `[Entity, Attribute, Value, Transaction, Assert/Retract]` tuples.
thus it is not a traditional table based database like SQL, but more like a RDF or graph database. To create what is
traditionally thought of as a table, you would query for all tuples with the same entity.
* **Typesafe**: The database is designed to be used with C# and is strongly typed. As much as possible, allocations are
removed from the inner parts of the application, and the database is designed to be used with value types. These values
are processed and sorted via C# code, so the database supports arbitrary types, as long as they can be compared.
* **In-process**: The database is designed to be used in the same process as the application, and is not a separate service,
and does not support multiple processes accessing the same database at the same time. Multiple threads within the same
process can access the database concurrently without issue.
* **Temporal**: The database supports the concept of time, and can be queried as of a specific time, or for all values
for a given entity, or attribute over type. In many ways this provides an audit log of all changes to the database. In
spite of this feature, an index is maintained for the "Current" view of the database so that most queries are fast, and
yet the full history is available.
* **Pluggable Storage**: The database is designed to support multiple storage backends, and the default storage backend is
RocksDB. However, the storage layer is abstracted, and any system that supports a sorted set of keys, iteration (forward
and backward), and atomic updates across multiple keys can be used. Currently, the only other storage backend is a in-memory
backed based on Microsoft's `System.Collections.Immutable` library which contains the `ImmutableSortedSet` class.

## Datamodel
At the core of the application is a set of attribute definitions. These are defined in C# as implementations of the `IAttribute`
class. Most often these are defined as classes inside a containing class to better group them by name. Here is an example
of a simple set of attributes:

```csharp
public class FileAttributes
{
    /// <summary>
    ///     The path of the file
    /// </summary>
    public class Path() : ScalarAttribute<Path, RelativePath>(isIndexed: true);

    /// <summary>
    ///     The size of the file
    /// </summary>
    public class Size : ScalarAttribute<Size, Paths.Size>;

    /// <summary>
    ///     The hashcode of the file
    /// </summary>
    public class Hash() : ScalarAttribute<Hash, Hashing.xxHash64.Hash>(isIndexed: true);

    /// <summary>
    ///     The mod this file belongs to
    /// </summary>
    public class ModId : ScalarAttribute<ModId, EntityId>;
}
```

A few interesting features of the above code:

* Each attribute is defined as a class, this is so the class can later be used in C# attributes such as `[From<Path>]`
for defining read models (more on this below).
* Attributes can be indexed, this is a hint to the database that this attribute will be queried on often, and it should
be included in the secondary or reverse index. In the above example all the entities where `Hash` is `X` can be found
very quickly, via a single lookup and scan of an index.
* Attributes define a value type. Also in the app, should be a `IValueSerializer<T>` for each of these value types. But
this system is open-ended. The serializer is used to convert the value to and from a byte array for storage in the database,
and to perform comparisons on the values, so longs need not be stored in BigEndian order as they are not compared as byte
arrays, but as longs. In addition, arbitrary types can be stored in the database, so long as they can be compared in their
C# form.


Once the attributes are defined, they must be registered with the DI container.

```csharp
services.AddAttributeCollection<FileAttributes>();
```

While attributes can be registered individually, it is recommended to group them into classes as shown above, and register
them at once via the `AddAttributeCollection` method.

## Read Models

For ease of use, MnemonicDB supports the concept of read models. These are classes that define a set of attributes that
are commonly queried together. There is nothing that requires these attributes to always be grouped in the same way, and
users are encouraged to define read models that make sense for their application.

```csharp
public class File(ITransaction? tx) : AReadModel<File>(tx)
{
    [From<FileAttributes.Path>]
    public required RelativePath Name { get; set; }

    [From<FileAttributes.Size>]
    public required Size Size { get; set; }

    [From<FileAttributes.Hash>]
    public required Hash Hash { get; set; }

    [From<FileAttributes.ModId>]
    public required EntityId ModId { get; init; }

    public static File Create(ITransaction tx, string name, Mod mod, Size size, Hash hash)
    {
        return new File(tx)
        {
            Name = name,
            Size = size,
            Hash = hash,
            ModId = mod.Id
        };
    }
}
```

The above class defines a read model for a file. It is a simple class with properties that are decorated with the `[From<Attribute>]`
attribute. This attribute is used to tell the database that when this read model is queried, it should include the values
for the given DB attributes. The `ITransaction` parameter is used during writes to the database, and can be ignored for now.

Now that the read model is defined, it can be used to query the database.

```csharp
    var file = db.Get<File>(eId);

    Console.WriteLine($"File: {file.Name} Size: {file.Size} Hash: {file.Hash}");
```

## Writing data
Datoms (tuples of data) are written to the database via transactions. Since MnemonicDB is a single-writer database, transactions
are not applied directly and do not lock the database. Instead, transactions are created, and then shipped off to the
writer task which serializes writes to the backing store.

Read models can be used to create new entities in the database. Here is an example of creating a new file entity using
the above constructor method.

```csharp
    var tx = db.BeginTransaction();
    var file = File.Create(tx, "file.txt", mod, 1024, hash);
    var txResult = await tx.Commit();
```

A key thing to note in this code is that each entity when it is created fresh, is given a temporary ID. These ids are
not unique, but are unique to the given transaction. Once `txResult` is returned, the tempId assigned to the entity
can be resolved to a real ID:

```csharp
    file = db.Get<File>(txResult[file.Id]);
```


