---
hide:
  - toc
---

<div align="center">
	<h1>MnemonicDB a in-process temporal database for .NET</h1>
	<img src="./Nexus/Images/Nexus-Icon.png" width="150" align="center" />
	<br/> <br/>
    <br/>
    <img alt="GitHub Workflow Status" src="https://img.shields.io/github/actions/workflow/status/Nexus-Mods/NexusMods.MnemonicDB/dotnet-build-and-test.yaml">
</div>

## About

Built for the NexusMods.App project, MnemonicDB is a tuple oriented, typesafe, in-process temporal database for .NET
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

## Conceptual Overview
MnemonicDB stores data (as mentioned) in tuples of `[Entity, Attribute, Value, Transaction, Assert/Retract]`. *Everything*
in the database is stored on-disk in this format. This includes the schema, the indexes, and the data itself. Thus defining
a datamodel in this database starts by defining attributes that will be collected into "models".

## Defining Attributes
Attributes are simply implementations of the `IAttribute` interface, and must inherit from the `Attribute<THighLevel, TLowLevel>` abstract
class. There is a lot of logic in these classes however, so it is recommended to use one of the provided helper classes such
as `ScalarAttribute<T>` or `ReferenceAttribute<T>`. In the definition of `Attribute<THighLevel, TLowLevel>`, `THighLevel` refers
to the C# type that the attribute will contain values of, and `TLowLevel` refers to the type that the database will use to
store the values. The attribute itself contains conversions to and from these types.

```csharp
var attr = new BooleanAttribute("Test.Namespace", "IsTest") { IsIndexed = true };
```

In this example a new attribute is defined with the name `Test.Namespace/IsTest`. Names in MneumonicDB are in the format of
`namespace/name` and are used to uniquely identify the attribute. The `IsIndexed` property is a hint to the database that
this attribute will be queried on often, and it should be included in the secondary or reverse index.

However, now what the attribute is created, how is it used? It can't yet, instead it must be registered with the DI container.
When the database starts it will query all the instances of `IAttribute` in the container and register them with the database.

A shorthand for this is to make the attributes static members of a class, and then register them all at once with the `AddAttributeCollection`
extension method:

```csharp
public class Person
{
    public const string Namespace = "Test.Model.Person";

    public static readonly StringAttribute Name = new(Namespace, nameof(Name)) { IsIndexed = true };
    public static readonly UInt32Attribute Age = new(Namespace, nameof(Age));

}

services.AddAttributeCollection(typeof(Person));
```

By convention, the namespace is stored in a constant string, and the attributes are stored as static readonly members of the
class, using the `nameof` operator to get the name of the attribute.

## Using Source Generators
Since a lot of code is required to easily define attributes, a source generator is provided to generate this code for you.
To define such a model, simply subclass your model from `IModelDefinition`. Be sure to include the `partial` keyword on
the class definition, as the source generator will generate the other half of the class.

```csharp
public partial class Person : IModelDefinition
{
    private const string Namespace = "Test.Person";
    public static readonly StringAttribute Name = new(Namespace, nameof(Path)) {IsIndexed = true};
    public static readonly UInt32Attribute Age = new(Namespace, nameof(Age));
}
```

Once the source generator runs, you will find that a lot of helper methods and code has been added to the class, including:

### Lookup Methods
The methods `.All()`, and `.Get()` are added to the class. These methods can be used to look up all models of this type, or
just a specific model by its entity ID.

### Write Model
A `Person.New` class is created so that new instances of this model can be created easily:

```csharp

var txn = db.NewTransaction();
var person = Person.New(txn)
{
    Name = "Test",
    Age = 32
};

await txn.Commit();

```
Every member of the model is required (unless the data is optional), and the `New` method will return a new instance of the
model that has been attached to the transaction. Committing the transaction will write the data to the database.

### ReadOnly Model
A `Person.ReadOnly` class is created so that instances of this model can be read from the database, these are read-only methods
and are returned by any methods that query the database. The `.Remap` method on the `.New` class will return a `ReadOnly` instance
given a transaction result.

!!!info
    During creation of a new entity, the entity ID is not known until the transaction is committed. Thus the `.New` class
will often have a `.Id` that has a `Partition` type of `Tmp`, meaning it's a temporary Id and never exists in the database
as that specific id. During the commit, the logging methods in the database will assign a new entity ID for each used Temporary
id, and those will be returned in the transaction result. The `.Remap` method will then replace the temporary id with the
newly assigned id, and return a `ReadOnly` instance of the model.

```csharp

var txn = db.NewTransaction();
var personNew = Person.New(txn)
{
    Name = "Test",
    Age = 32
};

var result = await txn.Commit();

var person = personNew.Remap(result);

// person.Name == "Test"
// person.Age == 32
// person.Id != personNew.Id

```
