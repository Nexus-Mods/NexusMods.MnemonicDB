using System;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;

namespace NexusMods.MnemonicDB.Abstractions.Attributes;

/// <summary>
/// An attribute that holds an enum value backed by an int32 value.
/// </summary>
[PublicAPI]
public sealed class EnumAttribute<T>(string ns, string name) : ScalarAttribute<T, int, Int32Serializer>(ns, name)
    where T : Enum
{
    /// <inheritdoc />
    public override int ToLowLevel(T value)
    {
        // NOTE(halgari): Looks like an allocation, but the cast to object is removed by the JIT since the type of
        // T is a compile-time constant. Verified via sharpLab.io:
        // https://sharplab.io/#v2:EYLgtghglgdgNAFxBAzmAPgAQEwEYCwAUJgAwAEAygBYQBOADgDITAB0ASgK4wJRgCmAbiJEA2gCkoCAOL8Y/WlADGACgQBPevwD2AMxUBZdQFEYnMAEoLAXTKYAzHexkAwgB4AKgD4yAdyoK/GQeZCBkpuZkAN5EZHHxDmSwCGQGKiEAbhAANpz8FtGx8cWYAOxkKskWKtrAAFb8SggWWblCRXEAvkTdhESJcpFGEWDRZABi2tpkALxkJHBkAEJ0s2S4ZJ2CQA=
        return (int)(object)value;
    }

    /// <inheritdoc />
    public override T FromLowLevel(int value, AttributeResolver resolver)
    {
        // Same as ToLowLevel, the cast to object is removed by the JIT
        return (T)(object)value;
    }
}

/// <summary>
/// An attribute that holds an enum value backed by a byte value.
/// </summary>
[PublicAPI]
public sealed class EnumByteAttribute<T>(string ns, string name) : ScalarAttribute<T, byte, UInt8Serializer>(ns, name)
    where T : Enum
{
    /// <inheritdoc />
    public override byte ToLowLevel(T value)
    {
        // NOTE(halgari): Looks like an allocation, but the cast to object is removed by the JIT since the type of
        // T is a compile-time constant. Verified via sharpLab.io:
        // https://sharplab.io/#v2:EYLgtghglgdgNAFxBAzmAPgAQEwEYCwAUJgAwAEAygBYQBOADgDITAB0ASgK4wJRgCmAbiJEA2gCkoCAOL8Y/WlADGACgQBPevwD2AMxUBZdQFEYnMAEoLAXTKYAzHexkAwgB4AKgD4yAdyoK/GQeZCBkpuZkAN5EZHHxDmSwCGQGKiEAbhAANpz8FtGx8cWYAOxkKskWKtrAAFb8SggWWblCRXEAvkTdhESJcpFGEWDRZABi2tpkALxkJHBkAEJ0s2S4ZJ2CQA=
        return (byte)(object)value;
    }

    /// <inheritdoc />
    public override T FromLowLevel(byte value, AttributeResolver resolver)
    {
        // Same as ToLowLevel, the cast to object is removed by the JIT
        return (T)(object)value;
    }
}
