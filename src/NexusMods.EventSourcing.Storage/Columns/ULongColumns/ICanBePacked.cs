using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Reloaded.Memory.Extensions;

namespace NexusMods.EventSourcing.Storage.Columns.ULongColumns;

public interface ICanBePacked<T> : IUnpacked<T>
    where T : struct
{
    public IReadable<T> Pack()
    {

    }



}
