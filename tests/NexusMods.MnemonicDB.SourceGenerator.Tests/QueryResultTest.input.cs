using NexusMods.HyperDuck;

namespace NexusMods.MnemonicDB.SourceGenerator.Tests;

[QueryResult]
public record struct MyStruct(string Foo, string Bar, int Baz);

[QueryResult]
public record struct MyClass(string Foo, string Bar, int Baz);
