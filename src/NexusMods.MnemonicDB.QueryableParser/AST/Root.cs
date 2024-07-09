using System.Reflection;

namespace NexusMods.MnemonicDB.QueryableParser.AST;

public record Root(MethodInfo Method, LVar To) : INode;
