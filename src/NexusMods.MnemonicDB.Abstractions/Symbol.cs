using System;
using System.Collections.Concurrent;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
///     A string that is interned behind a class so we can do reference equality
///     instead of value equality
/// </summary>
public class Symbol : IEquatable<Symbol>
{
    private static readonly ConcurrentDictionary<(string Namespace, string Name), Symbol> InternedSymbols = new();
    
    /// <summary>
    /// Cached hashcode for faster equality comparisons
    /// </summary>
    private readonly int _hashCode;

    /// <summary>
    ///     The constructor, which is private to ensure that all symbols are interned
    /// </summary>
    /// <param name="nsAndName"></param>
    private Symbol((string Namespace, string Name) nsAndName)
    {
        Id = $"{nsAndName.Namespace}/{nsAndName.Name}";
        _hashCode = Id.GetHashCode();
        Name = nsAndName.Name;
        Namespace = nsAndName.Namespace;
    }

    /// <summary>
    ///     Placeholder for unknown symbols
    /// </summary>
    public static Symbol Unknown => Intern("<unknown>");

    /// <summary>
    ///     The namespace of the symbol, the part before the name
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    ///     The name of the symbol
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     The full string name of the symbol, in the format of "Namespace/Name"
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///     Construct a new symbol, or return an existing one that matches the given name
    /// </summary>
    public static Symbol Intern(string fullName)
    {
        var nsAndName = Sanitize(fullName);
        if (InternedSymbols.TryGetValue(nsAndName, out var symbol))
            return symbol;

        symbol = new Symbol(nsAndName);
        return !InternedSymbols.TryAdd(nsAndName, symbol) ? InternedSymbols[nsAndName] : symbol;
    }

    /// <summary>
    /// Construct a new symbol, or return an existing one that matches the given namespace and name
    /// </summary>
    public static Symbol Intern(string nsStr, string nameStr)
    {
        var nsAndName = (nsStr, nameStr);
        if (InternedSymbols.TryGetValue(nsAndName, out var symbol))
            return symbol;

        symbol = new Symbol(nsAndName);
        return !InternedSymbols.TryAdd(nsAndName, symbol) ? InternedSymbols[nsAndName] : symbol;
    }

    /// <summary>
    ///     Construct a new symbol, or return an existing one that matches the given name
    /// </summary>
    public static Symbol InternPreSanitized(string fullName)
    {
        var split = fullName.Split("/");
        var nsAndName = (split[0], split[1]);
        if (InternedSymbols.TryGetValue(nsAndName, out var symbol))
            return symbol;

        symbol = new Symbol(nsAndName);
        return !InternedSymbols.TryAdd(nsAndName, symbol) ? InternedSymbols[nsAndName] : symbol;
    }

    private static (string Namespace, string Name) Sanitize(string nsAndName)
    {
        nsAndName = nsAndName.Replace("+", ".").Replace("/", ".");
        var lastDot = nsAndName.LastIndexOf('.');
        if (lastDot == -1)
            return ("<unknown>", nsAndName);
        return (nsAndName[..lastDot], nsAndName[(lastDot + 1)..]);
    }

    /// <inheritdoc />
    public override string ToString() 
        => Id;

    /// <inheritdoc />
    public override bool Equals(object? obj) 
        => ReferenceEquals(this, obj);

    /// <inheritdoc />
    public override int GetHashCode() 
        => _hashCode;

    /// <inheritdoc />
    public bool Equals(Symbol? other) 
        => ReferenceEquals(this, other);
}
