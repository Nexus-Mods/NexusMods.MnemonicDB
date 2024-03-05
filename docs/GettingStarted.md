---
hide:
  - toc
---

## Getting Started

Good examples are often worth a thousand descriptions so let's start with a simple example. First of all we need to define a set
of attributes that we want to collect into a model. These attributes have a type and are defined in code as a class. These classes
inherit all their logic from predefined abstract classes so their definitions are simple. Each attribute is backed by a "symbol"
which is a value type that contains a unique string that is the name of the attribute. This symbol is used to identify the attribute
uniquely in the database. By default the symbol is detected by the name and namespace of the class. So let's define a set
attributes for files and mods that will contain these files:


```csharp
public static class FileAttributes
{
    public class Hash : ScalarAttribute<Hash, ulong>;
    public class Size : ScalarAttribute<Size, ulong>;
    public class Name : ScalarAttribute<Name, string>;
    public class ModId : ScalarAttribute<ModId, EntityId>;
}

public static class ModAttributes
{
    public class Name : ScalarAttribute<Name, string>;
    public class Enabled : ScalarAttribute<Enabled, bool>;
}
```

!!!info
    Putting all attributes as child classes inside a static class is a convention, not a requirement. They are put this way
in this example so that it's clear that `Name` on a file is different from `Name` on a mod. Although, it's possible to put
the same attribute on multiple entities, this is not recommended as it removes the ability to quickly query for all files
based purely on a single attribute. Don't over generalize your attributes, it's better to have a few more attributes than
to make the model too complex.

So now that we have attributes we have to register them in the DI container. Currently we have to register these one by one,
but in the future we could easily register all attributes on a given static class.

```csharp
public static IServiceCollection AddAttributes(this IServiceCollection services)
{
    services.AddAttribute<FileAttributes.Hash>();
    services.AddAttribute<FileAttributes.Size>();
    services.AddAttribute<FileAttributes.Name>();
    services.AddAttribute<FileAttributes.ModId>();
    services.AddAttribute<ModAttributes.Name>();
    services.AddAttribute<ModAttributes.Enabled>();
    return services;
}
```

While we could go and insert datoms now, the interface is verbose and not very user friendly, instead we will now group
these attributes together into a "read model". Here is a simple example of a read model:

```csharp
public class File(ITransaction? tx) : AReadModel<File>(tx)
{
    [From<FileAttributes.Hash>]
    public required ulong Hash { get; init; }

    [From<FileAttributes.Size>]
    public required ulong Size { get; init; }

    [From<FileAttributes.Name>]
    public required string Name { get; init; }

    [From<FileAttributes.ModId>]
    public required EntityId ModId { get; init; }
}

public class Mod(ITransaction? tx) : AReadModel<Mod>(tx)
{
    [From<ModAttributes.Name>]
    public required string Name { get; set; }

    [From<ModAttributes.Enabled>]
    public required bool Enabled { get; set; }
}
```

From here we can create a connection and insert the datoms. The connection is the central mutation point for the database,
and it's most often injected via the DI framework:

```
IConnection connection = serviceProvider.GetRequiredService<IConnection>();

var tx = connection.BeginTransaction();

var mod = new Mod(tx) { Name = "My Mod", Enabled = true };

var file = new File(tx) { Hash = 123, Size = 456, Name = "My File", ModId = mod.Id };

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

mod = db.Get<Mod>(result[mod.Id]);
mod.Name.Should().Be("My Mod");

file = db.Get<File>(result[file.Id]);
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



