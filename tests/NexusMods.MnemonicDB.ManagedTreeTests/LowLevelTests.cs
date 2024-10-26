using System.Text;
using NexusMods.MnemonicDB.ManagedTree;
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

    private void Shuffle(string[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            var from = Random.Shared.Next(array.Length);
            var to = Random.Shared.Next(array.Length);

            (array[from], array[to]) = (array[to], array[from]);
        }
    }
    
    [Fact]
    public void CanAddAndSortStringData()
    {
        var strings = Enumerable.Range(0, 100)
            .Select(e => $"I{e}")
            .ToArray();
        
        var cpy = (string[])strings.Clone();
        Shuffle(cpy);
        
        var snapshot = CommitData(cpy);

        var iterator = snapshot.GetIterator();
        
        iterator.Start();
        
        var i = 0;

        AsArray(iterator).Should().BeEquivalentTo(strings, opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void CanAddAdditionalData()
    {
        CommitData(["I4", "I10", "I33"]);
        var snapshot = CommitData(["I1", "I5", "I11", "I12", "I34", "I4"]);
        var iterator = snapshot.GetIterator();
        iterator.Start();

        AsArray(iterator).Should().BeEquivalentTo("I1", "I4", "I5", "I10", "I11", "I12", "I33", "I34");
    }
    
    [Fact]
    public void CanRemoveData()
    {
        CommitData(["I4", "I10", "I33"]);
        var snapshot = CommitData([], ["I10", "I4"]);
        var iterator = snapshot.GetIterator();
        iterator.Start();

        AsArray(iterator).Should().BeEquivalentTo("I33");
    }
    
    private ISnapshot CommitData(string[] add, string[]? remove = null)
    {
        using var batch = new WritableBlock();
        var encoding = Encoding.UTF8;
        
        foreach (var s in add)
        {
            var size = encoding.GetByteCount(s) + 1;
            var span = batch.GetSpan(size);
            span[0] = 1;
            encoding.TryGetBytes(s, span.Slice(1), out var written);
            batch.Advance(written + 1);
            batch.NextRow();
        }

        foreach (var s in remove ?? [])
        {
            var size = encoding.GetByteCount(s) + 1;
            var span = batch.GetSpan(size);
            span[0] = 0;
            encoding.TryGetBytes(s, span.Slice(1), out var written);
            batch.Advance(written + 1);
            batch.NextRow();
        }
        
        var snapshot = _tree.Commit(batch);
        return snapshot;
    }

    private List<string> AsArray(IIterator iterator)
    {
        var result = new List<string>();
        while (iterator.MoveNext())
        {
            result.Add(Encoding.UTF8.GetString(iterator.Current));
        }

        return result;
    }
}
