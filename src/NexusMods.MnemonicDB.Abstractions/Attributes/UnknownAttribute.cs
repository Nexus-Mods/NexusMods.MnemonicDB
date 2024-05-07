using System;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An unknown attribute is an attribute that exists in the database but is not known to the application. This is
/// either because the attribute was removed, or didn't show up in the DI system.
/// </summary>
/// <param name="dbAttribute"></param>
/// <typeparam name="TLowLevel"></typeparam>
public class UnknownAttribute<TLowLevel>(DbAttribute dbAttribute) :
    Attribute<TLowLevel, TLowLevel>(dbAttribute.LowLevelType, dbAttribute.UniqueId.Namespace, dbAttribute.UniqueId.Name)
{
    /// <inheritdoc />
    protected override TLowLevel ToLowLevel(TLowLevel value)
    {
        return value;
    }

    /// <inheritdoc />
    protected override TLowLevel FromLowLevel(byte value, ValueTags tags)
    {
        if (tags != LowLevelType)
        {
            throw new ArgumentException($"Cannot convert {tags} to {LowLevelType}");
        }

        return (TLowLevel)(object)value;
    }

    /// <inheritdoc />
    protected override TLowLevel FromLowLevel(ushort value, ValueTags tags)
    {
        if (tags != LowLevelType)
        {
            throw new ArgumentException($"Cannot convert {tags} to {LowLevelType}");
        }

        return (TLowLevel)(object)value;
    }

    /// <inheritdoc />
    protected override TLowLevel FromLowLevel(uint value, ValueTags tags)
    {
        if (tags != LowLevelType)
        {
            throw new ArgumentException($"Cannot convert {tags} to {LowLevelType}");
        }

        return (TLowLevel)(object)value;
    }


    /// <inheritdoc />
    protected override TLowLevel FromLowLevel(ulong value, ValueTags tags)
    {
        if (tags != LowLevelType)
        {
            throw new ArgumentException($"Cannot convert {tags} to {LowLevelType}");
        }

        return (TLowLevel)(object)value;
    }

    /// <inheritdoc />
    protected override TLowLevel FromLowLevel(short value, ValueTags tags)
    {
        if (tags != LowLevelType)
        {
            throw new ArgumentException($"Cannot convert {tags} to {LowLevelType}");
        }

        return (TLowLevel)(object)value;
    }

    /// <inheritdoc />
    protected override TLowLevel FromLowLevel(int value, ValueTags tags)
    {
        if (tags != LowLevelType)
        {
            throw new ArgumentException($"Cannot convert {tags} to {LowLevelType}");
        }

        return (TLowLevel)(object)value;
    }

    /// <inheritdoc />
    protected override TLowLevel FromLowLevel(long value, ValueTags tags)
    {
        if (tags != LowLevelType)
        {
            throw new ArgumentException($"Cannot convert {tags} to {LowLevelType}");
        }

        return (TLowLevel)(object)value;
    }

    /// <inheritdoc />
    protected override TLowLevel FromLowLevel(float value, ValueTags tags)
    {
        if (tags != LowLevelType)
        {
            throw new ArgumentException($"Cannot convert {tags} to {LowLevelType}");
        }

        return (TLowLevel)(object)value;
    }

    /// <inheritdoc />
    protected override TLowLevel FromLowLevel(double value, ValueTags tags)
    {
        if (tags != LowLevelType)
        {
            throw new ArgumentException($"Cannot convert {tags} to {LowLevelType}");
        }

        return (TLowLevel)(object)value;
    }

    /// <inheritdoc />
    protected override TLowLevel FromLowLevel(string value, ValueTags tags)
    {
        if (tags != LowLevelType)
        {
            throw new ArgumentException($"Cannot convert {tags} to {LowLevelType}");
        }

        return (TLowLevel)(object)value;
    }
}

/// <summary>
/// Helper class to create unknown attributes
/// </summary>
public static class UnknownAttribute
{
    /// <summary>
    /// Creates an unknown attribute from a database attribute
    /// </summary>
    public static IAttribute Create(DbAttribute dbAttribute)
    {
        return dbAttribute.LowLevelType switch
        {
            ValueTags.Null => new UnknownAttribute<Null>(dbAttribute),
            ValueTags.UInt8 => new UnknownAttribute<byte>(dbAttribute),
            ValueTags.UInt16 => new UnknownAttribute<ushort>(dbAttribute),
            ValueTags.UInt32 => new UnknownAttribute<uint>(dbAttribute),
            ValueTags.UInt64 => new UnknownAttribute<ulong>(dbAttribute),
            ValueTags.UInt128 => new UnknownAttribute<ulong>(dbAttribute),
            ValueTags.Int16 => new UnknownAttribute<short>(dbAttribute),
            ValueTags.Int32 => new UnknownAttribute<int>(dbAttribute),
            ValueTags.Int64 => new UnknownAttribute<long>(dbAttribute),
            ValueTags.Int128 => new UnknownAttribute<long>(dbAttribute),
            ValueTags.Float32 => new UnknownAttribute<float>(dbAttribute),
            ValueTags.Float64 => new UnknownAttribute<double>(dbAttribute),
            ValueTags.Ascii => new UnknownAttribute<string>(dbAttribute),
            ValueTags.Utf8 => new UnknownAttribute<string>(dbAttribute),
            ValueTags.Utf8Insensitive => new UnknownAttribute<string>(dbAttribute),
            ValueTags.Blob => new UnknownAttribute<byte[]>(dbAttribute),
            ValueTags.HashedBlob => new UnknownAttribute<byte[]>(dbAttribute),
            ValueTags.Reference => new UnknownAttribute<string>(dbAttribute),
            _ => throw new ArgumentOutOfRangeException(nameof(dbAttribute.LowLevelType), dbAttribute.LowLevelType, null)
        };
    }
}
