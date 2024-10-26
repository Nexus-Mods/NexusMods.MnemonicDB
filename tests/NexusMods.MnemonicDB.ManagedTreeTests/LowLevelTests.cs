using System.Text;
using NexusMods.MnemonicDB.ManagedTree.Abstractions;

namespace NexusMods.MnemonicDB.ManagedTreeTests;

public class LowLevelTests
{
    private readonly ManagedTree.ManagedTree<StringComparer> _tree;

    public LowLevelTests()
    {
        _tree = new ManagedTree.ManagedTree<StringComparer>();
    }

    private class StringComparer : ISpanComparer
    {
        public static int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
        {
            var a = Encoding.UTF8.GetString(x);
            var b = Encoding.UTF8.GetString(y);

            var cmp = a[0].CompareTo(b[0]);
            if (cmp != 0)
                return cmp;
            
            return Int32.Parse(a[1..]).CompareTo(Int32.Parse(b[1..]));
        }
    }
    
    [Fact]
    public void CanAddAndSortStringData()
    {
        var strings = Enumerable.Range(0, 100)
            .Select(e => $"I{e}")
            .ToArray();

        var batch = _tree.CreateWriteBatch();
        foreach (var s in strings)
        {
            batch.Add(Encoding.UTF8.GetBytes(s));
        }
        var snapshot = batch.Commit();
        
        var iterator = snapshot.GetIterator();
        
        iterator.Start();
        
        var i = 0;

        while (iterator.MoveNext())
        {
            var key = Encoding.UTF8.GetString(iterator.Current);
            key.Should().Be(strings[i]);
            i++;
        }


    }
}
