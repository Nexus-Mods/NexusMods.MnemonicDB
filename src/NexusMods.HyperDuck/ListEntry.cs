using System.Runtime.InteropServices;

namespace NexusMods.HyperDuck;

[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct ListEntry
{
    public ulong Offset;
    public ulong Length;
}