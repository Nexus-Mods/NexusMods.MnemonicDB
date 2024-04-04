---
hide:
  - toc
---


### Making changes to the schema

New attributes can be added to the database at any time, and the database will automatically start indexing them. In addition,
old attributes need not remain in the C# codebase, MnemonicDB will simply skip over them when loading values. So as much
as possible, try to make additive changes to the schema, and avoid changing attributes. Attributes are named after the classes
by convention, but this is not a requirement, and the database will work just fine if you change the class name of an attribute,
as long as the attribute's unique ID remains the same. Thus deprecated attributes can be moved to a `Deprecated` namespace, and
left to sit.

### Migrations

Migrations are not yet implemented, but the idea is fairly simple, a new database is created, and the TxLog of the source
is replayed into the target with some sort of transformation process happening on the way. This is a future feature, and
planned to be implemented soon.
