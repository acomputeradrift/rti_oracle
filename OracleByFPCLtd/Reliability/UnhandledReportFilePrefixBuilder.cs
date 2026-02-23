using System;
using System.IO;
using System.Text;

namespace OracleByFPCLtd.Reliability;

public static class UnhandledReportFilePrefixBuilder
{
    public static string Build(string? projectFilePath)
    {
        const string basePrefix = "Oracle_Unhandled";
        if (string.IsNullOrWhiteSpace(projectFilePath))
        {
            return basePrefix;
        }

        var projectName = Path.GetFileNameWithoutExtension(projectFilePath);
        if (string.IsNullOrWhiteSpace(projectName))
        {
            return basePrefix;
        }

        var cleaned = SanitizeFileNameSegment(projectName);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return basePrefix;
        }

        return $"{basePrefix}_{cleaned}";
    }

    private static string SanitizeFileNameSegment(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (Array.IndexOf(invalidChars, ch) >= 0)
            {
                continue;
            }

            builder.Append(ch == ' ' ? '_' : ch);
        }

        return builder.ToString().Trim('_');
    }
}
