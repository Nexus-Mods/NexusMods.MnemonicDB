using System.Reflection;

namespace NexusMods.MnemonicDB.QueryableParser.AST;

public record PropertyAccess(LVar Source, PropertyInfo Property, LVar Output) : INode;
