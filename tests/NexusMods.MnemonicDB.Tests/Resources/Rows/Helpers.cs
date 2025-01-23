using System.Runtime.InteropServices;
using NexusMods.Hashing.xxHash3;

namespace NexusMods.MnemonicDB.Tests.Resources.Rows;

public static class Helpers
{
    public static Hash HashFromBase64(string base64)
    {
        var hashBytes = Convert.FromBase64String(base64);
        var hash = Hash.From(MemoryMarshal.Read<ulong>(hashBytes));
        return hash;
    }

}
