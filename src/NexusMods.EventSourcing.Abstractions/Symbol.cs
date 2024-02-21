using System.Collections.Concurrent;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A string that is interned behind a class so we can do reference equality
/// instead of value equality
/// </summary>
public class Symbol
{
    /// <summary>
    /// The constructor, which is private to ensure that all symbols are interned
    /// </summary>
    /// <param name="nsAndName"></param>
    private Symbol(string nsAndName)
    {
        nsAndName = nsAndName.Replace("+", ".");
        Id = nsAndName;
        var splitOn = nsAndName.LastIndexOf('.');
        Name = nsAndName[(splitOn + 1)..];
        Namespace = nsAndName[..splitOn];
    }

    private static readonly ConcurrentDictionary<string, Symbol> InternedSymbols = new();

    /// <summary>
    /// Construct a new symbol, or return an existing one that matches the given name
    /// </summary>
    /// <param name="nsAndName"></param>
    /// <returns></returns>
    public static Symbol Intern(string nsAndName)
    {
        if (InternedSymbols.TryGetValue(nsAndName, out var symbol))
            return symbol;

        symbol = new Symbol(nsAndName);
        return !InternedSymbols.TryAdd(nsAndName, symbol) ? InternedSymbols[nsAndName] : symbol;
    }

    /// <summary>
    /// The namespace of the symbol, the part before the name
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// The name of the symbol
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The full string name of the symbol, in the format of "Namespace/Name"
    /// </summary>
    public string Id { get; }
}
