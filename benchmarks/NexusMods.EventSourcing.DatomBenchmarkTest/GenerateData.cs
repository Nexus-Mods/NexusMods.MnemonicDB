using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static NexusMods.EventSourcing.DatomBenchmarkTest.Attributes;

namespace NexusMods.EventSourcing.DatomBenchmarkTest;

public class GenerateData(DatomStore store)
{

    private static ulong entId = 0;
    private static ulong TX = 0;
    public ulong EmitCount = 0;

    public void Generate()
    {
        Dictionary<(int Loadout, int Mod), ulong> modIds = new();

        ulong txId = 0;

        for (var loadoutNum = 0; loadoutNum < MAX_LOADOUTS; loadoutNum++)
        {
            Console.WriteLine($"Loadout {loadoutNum}");
            var loadoutId = entId++;
            Emit(loadoutId, (uint)Loadout_Name, $"Loadout {loadoutNum}");

            var modsJoinId = entId++;
            Emit(loadoutId, (uint)Loadout_Mods, modsJoinId);

            for (var modNum = 0; modNum < MAX_MODS; modNum++)
            {
                var modId = entId++;

                modIds.Add((loadoutNum, modNum), modId);

                TX += 1;

                Emit(modsJoinId, (uint)modsJoinId, modId);
                Emit(modId, (uint)Mod_Name, $"Mod {modNum}");
                Emit(modId, (uint)Mod_Enabled, true);

                var filesModJoinId = entId++;
                Emit(modId, (uint)Mod_Files, filesModJoinId);

                foreach (var fileNum in Enumerable.Range(0, MAX_FILES))
                {
                    var fileId = entId++;
                    Emit(filesModJoinId, (uint)filesModJoinId, fileId);
                    Emit(fileId, (uint)Mod_File_Name, $"File {fileNum}");
                    Emit(fileId, (uint)Mod_File_Hash, (ulong)fileNum);
                }
                TX += 1;
            }
        }

        var lst = modIds.ToArray();

        for (var shuffleCount = 0; shuffleCount < MAX_SUFFLE; shuffleCount++)
        {
            Console.WriteLine("Shuffle");
            Random.Shared.Shuffle(lst);

            for (var idx = 0; idx < TOGGLE_COUNT; idx++)
            {
                var (_, mod) = lst[idx];
                TX += 1;
                Emit(mod, (uint)Loadout_Name, $"MOD {shuffleCount} {mod}");
            }
        }

    }

    public void Emit(ulong ent, uint attr, bool val)
    {
        Span<byte> temp = stackalloc byte[sizeof(bool)];
        temp[0] = val ? (byte)1 : (byte)0;
        Emit(ent, attr, temp);
    }

    public void Emit(ulong ent, uint attr, ulong val)
    {
        Span<byte> temp = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt16BigEndian(temp, (ushort)val);
        Emit(ent, attr, temp);
    }

    public void Emit(ulong ent, uint attr, string val)
    {
        var size = Encoding.UTF8.GetByteCount(val);
        Span<byte> temp = stackalloc byte[size];
        Encoding.UTF8.GetBytes(val, temp);
        Emit(ent, attr, temp);
    }


    private void Emit(ulong ent, uint attr, ReadOnlySpan<byte> val)
    {
        store.InsertDatom( ent, attr, val, TX);
        EmitCount++;
    }

    private const int MAX_LOADOUTS = 100;
    private const int MAX_MODS = 1000;
    private const int MAX_FILES = 1;
    private const int TOGGLE_COUNT = 100;
    private const int MAX_SUFFLE = 1000;
}
