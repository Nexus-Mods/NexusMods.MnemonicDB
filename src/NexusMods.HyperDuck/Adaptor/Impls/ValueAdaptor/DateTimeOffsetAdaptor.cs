using System;

namespace NexusMods.HyperDuck.Adaptor.Impls.ValueAdaptor;

public class DateTimeOffsetTicksAdaptor : IValueAdaptor<DateTimeOffset>
{
    public static bool Adapt<TCursor>(TCursor cursor, ref DateTimeOffset value) where TCursor : IValueCursor, allows ref struct
    {
        var ticks = cursor.GetValue<long>();
        return Helpers.AssignNotEq(ref value, new DateTimeOffset(ticks, TimeSpan.Zero));
    }
}

public class DateTimeOffsetNanoAdaptor : IValueAdaptor<DateTimeOffset>
{
    public static bool Adapt<TCursor>(TCursor cursor, ref DateTimeOffset value) where TCursor : IValueCursor, allows ref struct
    {
        // NOTE(erri120): TIMESTAMP_NS is stored as nanoseconds since 1970-01-01 in int64_t.
        var nanoseconds = cursor.GetValue<long>();
        var ticksSinceUnixEpoch = nanoseconds / TimeSpan.NanosecondsPerTick;
        var utcTicks = DateTimeOffset.UnixEpoch.Ticks + ticksSinceUnixEpoch;
        return Helpers.AssignNotEq(ref value, new DateTimeOffset(utcTicks, TimeSpan.Zero));
    }
}

public class DateTimeOffsetAdaptorFactory : IValueAdaptorFactory
{
    public bool TryExtractType(DuckDbType taggedType, LogicalType logicalType, Type type, out Type[] subTypes, out int priority)
    {
        if ((taggedType is DuckDbType.BigInt or DuckDbType.TimestampNs) && type == typeof(DateTimeOffset))
        {
            priority = 1;
            subTypes = [];
            return true;
        }

        priority = 0;
        subTypes = [];
        return false;
    }

    public Type CreateType(Registry registry, DuckDbType taggedType, LogicalType logicalType, Type resultTypes, Type[] subTypes, Type[] subAdaptors)
    {
        if (taggedType is DuckDbType.TimestampNs) return typeof(DateTimeOffsetNanoAdaptor);
        if (taggedType is DuckDbType.BigInt) return typeof(DateTimeOffsetTicksAdaptor);
        throw new NotSupportedException();
    }
}
