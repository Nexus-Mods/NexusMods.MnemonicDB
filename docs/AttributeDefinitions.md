---
hide:
  - toc
---


## Attribute Definitions
The datoms stored in the store require typed attributes with unique name and serializer tagging. Storing this information in
a way that provides typed access to attribues is strangely difficult. There are several ways to tackle this issue, which we will talk about here.

### Symbolic Names
The simplest approach is to do what Datomic does and use symbolic names for attributes. For example a `:loadout/name` attribute
would be registered as a string type:

```csharp

    System.RegisterAttribute<string>("loadout/name");
    var nameSymbol = Symbol.Intern("loadout/name");

    tx.Add(eid, nameSymbol, "My Loadout");
```

The problem with this approach is there is nothing stopping someone from using the wrong type for an attribute. The error happens
at runtime instead of at compile time. In addition loading values from the store may result in boxing unless care is taken.

This approach does allow for the use of fairly dynamic queries however.

```csharp
var query = from e in db.Entities
            where e[Loadout_Name] == "My Loadout"
            select e.Pull(Loadout_Name, Loadout_Version);

// What is the type of query result?
var results = query.ToList();
```

!!!info
    Dapper uses this approach, as does Datomic, but it assumes a dynamic query result. This is not a bad thing, but it does
reduce the ability to use the type system to catch errors.

If we want to predefine a query model, we end up with something even more complex

```csharp

interface QueryResult
{
    string Name { get; }
    int Version { get; }
}


var results = from e in db.Entities<QueryResult>()
             where e.Name == "My Loadout"
             select e;
````

The question here is how we map the symbolic names to the result type. We can use attributes, but attributes must have
constant values as arguments, so we can't symbolc names and must use strings or some sort of `nameof` expression.

```csharp
interface QueryResult
{
    [From("loadout/name")]
    string Name { get; }
    [From(nameof(NexusMods.Model.Loadout_Version))]
    int Version { get; }
}
```

Since `nameof` only names the specific type, we have to give it a fully qualified name. This is also suboptimal.

### Attributes as Types

Another approach would be to use the type system to define the attributes. This has the advantage of being able to use the types
to provide strict type checking, and we can use attribues to provide the symbolic names.

```csharp
namespace Loadout {
   public class Name : Attribute<Name, string>();
   public class Version : Attribute<Version, int>();
}

public class QueryResult {
    [From<Name>]
    public string Name { get; }
    [From<File.Version>]
    public int Version { get; }
}
```

Unfortunately in this approach we have to make sure to use the correct type for the getter in the read model. There's nothing
stopping us from accidentally defining `Name` as an `int` for example. This would not result in a compile time error. If we pre-register
all our read models (like `QueryResult`) we can use reflection to check the types and at least we get a startup time error.

Another problem with this approach is that C#'s inference system is not good at resolving complex constraints, for example:

```csharp

// This requires us to know at usage time that Name is a string attribute, and to make sure that the type is correct.
tx.Add<Name, string>("foo");

// What we can do, is put the `.Bar` method in a static extension method

tx.Add<Name>("foo");

public static class AttributeExtensions
{
    public static void Add<T>(this Transaction tx, T attribute, string value)
        where T : Attribute<T, string>
    {
        tx.Add(attribute, value);
    }

    public static void Add<T>(this Transaction tx, T attribute, float value)
        where T : Attribute<T, float>
    {
        tx.Add(attribute, value);
    }
}
```

This works quite well, and we only need to perform the operation once per attribute value type. After we do this we can easily define
models that use the attributes.

```csharp

record ReadModel
{
    [From<Name>]
    public string Name { get; }
    [From<Version>]
    public int Version { get; }
}

record WriteModel
{
    [From<Name>]
    public required string Name { get; init;}
    [From<Version>]
    public required int Version { get; init;}
}

/// Define a model that responds to new transactions and fires INotifyPropertyChanged events
public class ActiveModel : ActiveModel
{
    [From<Name>]
    public string Name { get; set; }
    [From<Version>]
    public int Version { get; set; }
}

/// Perform an ad-hoc query
/// Find files with the path of "c:\temp\foo.txt" and look up the name and version of the mods that contain them.
var results = from f in db.Where<File.Name>(@"c:\temp\foo.txt")
              from m in db.Where<File.Mod>(f.EntityId)
              select db.Pull<ReadModel>(m.EntityId);
```


