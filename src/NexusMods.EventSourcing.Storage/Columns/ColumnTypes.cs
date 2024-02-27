using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace NexusMods.EventSourcing.Storage.Abstractions.Columns;

public static class  ColumnTypes
{
    public static readonly uint CONST_BYTE = MakeHeader("CSTB");


    private static uint MakeHeader(string header)
    {
        #if DEBUG
        if (header.Length != 4)
        {
            throw new ArgumentException("Header must be 4 characters long");
        }
        #endif

        Span<byte> span = stackalloc byte[sizeof(uint)];
        Encoding.ASCII.GetBytes(header, span);
        return BinaryPrimitives.ReadUInt32BigEndian(span);
    }

}
