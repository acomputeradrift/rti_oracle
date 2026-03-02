using System;
using System.Threading;

namespace OracleByFPCLtd.Logging;

public static class LogTimestampSource
{
    private static long _lastBinaryTimestamp;
    public static event Action<DateTime>? ProcessorTimestampUpdated;

    public static void Reset()
    {
        Interlocked.Exchange(ref _lastBinaryTimestamp, 0);
    }

    public static void UpdateProcessorTimestamp(DateTime timestamp)
    {
        Interlocked.Exchange(ref _lastBinaryTimestamp, timestamp.ToBinary());
        ProcessorTimestampUpdated?.Invoke(timestamp);
    }

    public static DateTime GetTimestamp(DateTime fallback)
    {
        var binary = Interlocked.Read(ref _lastBinaryTimestamp);
        return binary == 0 ? fallback : DateTime.FromBinary(binary);
    }

    public static bool TryGetTimestamp(out DateTime timestamp)
    {
        var binary = Interlocked.Read(ref _lastBinaryTimestamp);
        if (binary == 0)
        {
            timestamp = default;
            return false;
        }

        timestamp = DateTime.FromBinary(binary);
        return true;
    }
}
