using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace NexusMods.EventSourcing.Abstractions.Collections;

/// <summary>
/// A ObservableCollection that transforms the items from one type to another.
/// </summary>
/// <typeparam name="TFrom"></typeparam>
/// <typeparam name="TTo"></typeparam>
public class TransformingObservableCollection<TFrom, TTo> : ObservableCollection<TTo>
{
    /// <summary>
    /// A ObservableCollection that transforms the items from one type to another.
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="from"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public TransformingObservableCollection(Func<TFrom, TTo> transform, ObservableCollection<TFrom> from)
    {
        foreach (var item in from)
            Add(transform(item));

        from.CollectionChanged += (sender, args) =>
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in args.NewItems!)
                        Add(transform((TFrom)item));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in args.OldItems!)
                        Remove(transform((TFrom)item));
                    break;
                default:
                    throw new InvalidOperationException();
            }
        };
    }

}
