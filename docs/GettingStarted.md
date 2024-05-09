---
hide:
  - toc
---

## Getting Started

Good examples are often worth a thousand descriptions so let's start with a simple example. First of all we need to define a set
of attributes that we want to collect into a model. These attributes will be instances of the `Attribute<T>` type, and can be
annotated with various parameters to describe the attribute. Each attribute must have a unique name in the format of
`name.space/name`. This name is used inside the database to link the `AttributeId` to a unique name that persists across
database restarts. These attributes are commonly stored in a static class (being static is recommended so that the class isn't
accidentally instantiated).


```csharp
public static class File
{
    private const string Namespace = "Test.Model.File";
    
    public static readonly ScalarAttribute<ulong> Hash = new(Namespace, nameof(Hash), isIndexed: true);
    public static readonly ScalarAttribute<ulong> Size = new(Namespace, nameof(Size));
    public static readonly ScalarAttribute<string> Name = new(Namespace, nameof(Name), noHistory: true);
    public static readonly ReferenceAttribute<EntityId> ModId = new(Namespace, nameof(ModId), cardinality: Cardinality.Many);

    public class Model(ITransaction tx) : AEntity(tx)
    {
        public ulong Hash {
            get => File.Hash.Get(this);
            set => File.Hash.Set(this, value);

        }

        public ModId ModId {
            get => File.ModId.Get(this);
            set => File.ModId.Set(this, value);
        }

        public Mod.Model Mod {
            get => Db.Get<Mod.Model>(ModId);
            set => ModId = value.Id;
        }
    }
}
```

The attributes are defined in the outer class level as they are commonly used throughout the system. The `Model` class is a
loose collection of attributes grouped into a common projection (or entity). There's no requirement that the attributes
must be grouped together with themselves, and it's recommended to mix and match attributes from different definitions where
appropriate. Attribute definition requires a bit of design thought, as additional performance can be gained by having more
attributes to partition the data being queried, but at the same time, too many attributes can result in queries needing
to concatenate results from multiple attributes.

Attributes must also be registered in the DI container. This is done by calling `.AddAttributeCollection(typeof(AttrCollection))`.
This method will scan the class for all static attribute definitions and register them with the DI container.

```csharp
public static IServiceCollection AddAttributes(this IServiceCollection services)
{
    services.AddAttributeCollection(typeof(File));
    return services;
}
```

From here we can create a connection and insert the datoms. The connection is the central mutation point for the database,
and it's most often injected via the DI framework:

```
IConnection connection = serviceProvider.GetRequiredService<IConnection>();

using var tx = connection.BeginTransaction();

var file = new File(tx) { Hash = 123, Size = 456, Name = "My File", Mod = mod };

var result = await tx.Commit();
```

Since the connection contains a queue, we must await the commit to wait for our transaction to process and be inserted into the database.
When the entities are created, they are assigned temporary entity ids. The ids are not known at creation time, because they
are not known until the transaction is committed. This is a key feature of the system, as it allows us to create relationships
between entities without having to worry about the order of creation or clobbering other in-progress transactions.

The result object contains a mapper function for converting these temporary ids to the real ids. This is useful for when you
need to get the id of an entity that you just created. Based on all of this, we can now query the database for the mod and
file we just created:

```csharp

var db = connection.Db;

file = db.Get<File.Model>(result[file.Id]);
file.Name.Should().Be("My File");
file.ModId.Should().Be(mod.Id);
```

This interface may be improved in the future with more syntactic sugar (such as `mod = result[mod]` vs `mod = db.Get<Mod>(result[mod.Id]))`), but for now it's a good starting point.

!!!info
    The `Db` object is a read-only view of the database, and is not affected by any changes to the database. This is useful for
    querying the database without having to worry about the state of the database changing while you are querying it. The `Db`
    object is also thread safe, and can be used from multiple threads at the same time. Connections cannot be read or queried,
    instead they must be "dereferenced" by calling `conn.Db`, and from there the db object can be used for any number of queries
    which will all have the same base view of time.



