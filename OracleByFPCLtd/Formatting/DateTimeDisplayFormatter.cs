using System;
using System.Globalization;

namespace OracleByFPCLtd.Formatting;

public static class DateTimeDisplayFormatter
{
    public const string HighPrecisionDisplayPattern = "yy-MM-dd h:mm:ss.fff tt";
    public const string FilterDisplayPattern = "yy-MM-dd h:mm tt";

    private static readonly string[] HighPrecisionParsePatterns =
    {
        HighPrecisionDisplayPattern,
        "yyyy-MM-dd HH:mm:ss.fff"
    };

    private static readonly string[] FilterParsePatterns =
    {
        FilterDisplayPattern,
        "yyyy-MM-dd HH:mm"
    };

    public static string FormatHighPrecisionDisplay(DateTime value)
    {
        return value.ToString(HighPrecisionDisplayPattern, CultureInfo.InvariantCulture);
    }

    public static string FormatFilterDisplay(DateTime value)
    {
        return value.ToString(FilterDisplayPattern, CultureInfo.InvariantCulture);
    }

    public static bool TryParseFilterInput(string text, out DateTime value)
    {
        return DateTime.TryParseExact(
            text,
            FilterParsePatterns,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal,
            out value);
    }

    public static bool TryParseHighPrecisionInput(string text, out DateTime value)
    {
        return DateTime.TryParseExact(
            text,
            HighPrecisionParsePatterns,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal,
            out value);
    }
}
