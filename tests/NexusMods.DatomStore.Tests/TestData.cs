namespace NexusMods.DatomStore.Tests;

public static class TestData
{
    public static (ulong, ulong, ulong)[] UlongData(int datoms = 1024)
    {
        ulong idx = 0;

        var data = new (ulong, ulong, ulong)[datoms];

        for(var i = 0; i < datoms; i++)
        {
            data[i] = (idx++, idx++, idx++);
        }

        Random.Shared.Shuffle(data);
        return data;
    }
}
