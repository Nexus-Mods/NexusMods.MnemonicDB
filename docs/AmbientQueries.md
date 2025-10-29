## Ambient Queries
MnemonicDB's DuckDB integration supports the concept of ambient queries. These are SQL queries that are executed automatically
when the query engine is initialized. Note: The query engine is all in-memory, so these queries are not persisted across sessions,
and are not persisted to disk.

### Usage
To start, create a .sql file in your project. Include a `-- namespace:` comment at the top of the file; this
is used by the source generator to determine the C# namespace of the generated code.

You can also specify dependencies with one or more optional `-- requires:` lines to ensure that other ambient SQL namespaces
are loaded first.

```sql
-- ExampleQueries.sql
-- namespace: NexusMods.MnemonicDB.TestModel
-- requires: Some.Other.Namespace   -- optional, repeat as needed
CREATE MACRO FilesForLoadout(loadoutId, db) AS TABLE
       SELECT Id, Path FROM mdb_File(Db=>db) WHERE LoadoutId = loadoutId;
```

Now mark this file as an `AdditionalFiles` and `UpToDateCheckInput` in your project file. The first marks the file 
as an additional project file, and the second tells MSBuild to check for changes to the file whenever builds are run. The
MnemonicDB source generator will automatically include the file in the generated code.

```xml
      <AdditionalFiles Include="ExampleQueries.sql" />
      <UpToDateCheckInput Include="ExampleQueries.sql"/>  
```

Now you can include this code in your DI container:

```
 services.AddExampleQueriesSql();
```

The generated code carries the `requires` list into DI, and at runtime the registry loads all ambient SQL fragments in an order
that honors the declared dependencies by namespace. If a required namespace is not present, it is ignored.

Now whenever you perform a query, the `FilesForLoadout` macro will be available for usage. 
