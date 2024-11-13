using System;
using System.Collections.Generic;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;

namespace NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Extensions for IEnumerable
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Performs a sorted merge of two sequences using the specified comparer.
    /// </summary>
    public static IEnumerable<T> Merge<T>(this IEnumerable<T> one, IEnumerable<T> two, Func<T, T, int> comparer)
    {
        // Code from https://kevsoft.net/2020/09/25/combining-two-sorted-generic-enumerables-in-csharp.html
        using var enumeratorOne = one.GetEnumerator();
        using var enumeratorTwo = two.GetEnumerator();

        var moreOne = enumeratorOne.MoveNext();
        var moreTwo = enumeratorTwo.MoveNext();

        while (moreOne && moreTwo)
        {
            var compare = comparer(enumeratorOne.Current, enumeratorTwo.Current);
            if (compare <= 0)
            {
                yield return enumeratorOne.Current;
                moreOne = enumeratorOne.MoveNext();
            }
            else
            {
                yield return enumeratorTwo.Current;
                moreTwo = enumeratorTwo.MoveNext();
            }

        }

        if (moreOne | moreTwo)
        {
            var finalEnumerator = moreOne ? enumeratorOne : enumeratorTwo;

            yield return finalEnumerator.Current;
            while (finalEnumerator.MoveNext())
            {
                yield return finalEnumerator.Current;
            }
        }
    }
}
