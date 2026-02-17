using System;
using System.Threading;

namespace OracleByFPCLtd.Logging;

public static class LogTimestampSource
{
    private static long _lastBinaryTimestamp;

    public static void UpdateProcessorTimestamp(DateTime timestamp)
    {
        Interlocked.Exchange(ref _lastBinaryTimestamp, timestamp.ToBinary());
    }

    public static DateTime GetTimestamp(DateTime fallback)
    {
        var binary = Interlocked.Read(ref _lastBinaryTimestamp);
        return binary == 0 ? fallback : DateTime.FromBinary(binary);
    }
}
