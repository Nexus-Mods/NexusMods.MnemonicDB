---
hide:
  - toc
---

## Attributes and Projections

This framework is designed to be as flexible to complex data models, while also offering a clear view of changes over time. This is primarily
done through the use of attributes and projections. The data model is defined in code (and saved to the database) as a large bag of attributes.
These attributes are then grouped into logical views of data called projections.

### Attributes
Attributes are defined in code and registered in the dependency injection system. They are given a unique identifier and a storage type. This
unique id may not be the same as the `attribute` id in the database, but is mapped at runtime to the `ulong` defined in the data layer. This
allows for additive changes to the attibutes without having to re-index the entire system. A new module in the app my create new attributes
and they will be automatically be inserted into the database.

```csharp

static class ModAttributes {

    public static readonly Value<string> Name = new("cd628ed6-7b2c-45e1-8957-407765cded4c");
    public static readonly Value<string> Description = new("d3e3e3e3-7b2c-45e1-8957-407765cded4c");
    public static readonly Reference Loadout = new("d3e3e3e3-7b2c-45e1-8957-407765cded4c");
    public static readonly Value<bool> Enabled = new("d3e3e3e3-7b2c-45e1-8957-407765cded4c");


    public static void RegisterAttributes(IServiceCollection services) {
        services.AddAttribute(Name);
        services.AddAttribute(Description);
        services.AddAttribute(Loadout);
        services.AddAttribute(Enabled);
    }


    public static EntityId<Mod> Create(Transaction t, string name, EntityId Loadout)
    {
        var id = t.CreateEntity<Mod>();
        t.Assert(id, Name, name);
        t.Assert(id, Loadout, Loadout);
        t.Assert(id, Enabled, true);
        return id;
    }

    public static void SetEnabled(Transaction t, EntityId<Mod> id, bool enabled)
    {
        t.Assert(id, Enabled, enabled);
    }
}
```


Now that values are asserted, we need a way to query the values. This is done through projections.
