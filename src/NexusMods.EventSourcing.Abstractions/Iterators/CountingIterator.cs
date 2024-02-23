namespace NexusMods.EventSourcing.Abstractions.Iterators;

public class CountingIterator<TPrevious, T>(TPrevious previous, int max, int count = 0) : IIterator<T>
where TPrevious : IIterator<T>
{
    public bool Next()
    {
        if (count >= max) return false;
        if (!previous.Next()) return false;

        count++;
        return true;
    }

    public bool AtEnd => count == max || previous.AtEnd;

    public bool Value(out T value)
    {
        if (count < max)
        {
            return previous.Value(out value);
        }
        value = default!;
        return false;
    }
}
