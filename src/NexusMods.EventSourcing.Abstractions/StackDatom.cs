using System;
using System.Diagnostics;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
///     A datom that can only exist on the stack. This is used to move data between the various indexes in the storage
///     layer
/// </summary>
public ref struct StackDatom
{
    /// <summary>
    ///     The entity id
    /// </summary>
    public ulong E;

    /// <summary>
    ///     The Attribute id
    /// </summary>
    public ushort A;

    /// <summary>
    ///     The Transaction id
    /// </summary>
    public ulong T;

    /// <summary>
    ///     The value span
    /// </summary>
    public ReadOnlySpan<byte> V;

    /// <summary>
    ///     The span for the value, prefixed with PaddingSize bytes of padding, these bytes can be used to store
    ///     extra data that needs to be written to the database, but is not part of the value itself.
    /// </summary>
    public Span<byte> PaddedSpan;

    /// <summary>
    ///     Returns the padded span, with all but the last padding bytes removed
    /// </summary>
    public Span<byte> Padded(int padding)
    {
        Debug.Assert(padding is >= 0 and <= PaddingSize);
        return PaddedSpan.SliceFast(PaddingSize - padding);
    }

    /// <summary>
    ///     The size of the padding in the V span
    /// </summary>
    public const int PaddingSize = sizeof(ulong) + sizeof(ushort) + sizeof(ulong);
}
