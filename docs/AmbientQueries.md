## Ambient Queries
MnemonicDB's DuckDB integration supports the concept of ambient queries. These are SQL queries that are executed automaticall
when the query engine is initialized. Note: The query engine is all in-memory, so these queries are not persisted across sessions,
and are not persisted to disk.

### Usage
To start, create a .sql file in your project. Be sure to include a `-- namespace: ` comment at the top of the file, this
will be used by the source generator to determine the namespace of the generated code.

```sql
-- ExampleQueries.sql
-- namespace: NexusMods.MnemonicDB.TestModel
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

Now whenever you perform a query, the `FilesForLoadout` macro will be available for usage. 
