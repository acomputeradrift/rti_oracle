using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OracleByFPCLtd;

public static class LogLevelAckParser
{
    private static readonly Regex DriverParenPattern = new(
        @"Setting LogLevel on DRIVER\s*\((\d+)\)\s*to\s*(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex DriverDirectPattern = new(
        @"Setting LogLevel on\s+(DRIVER//\d+)\s*to\s*(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ChannelPattern = new(
        @"Setting LogLevel on\s+([A-Z0-9_]+)\s*to\s*(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool TryParse(string? text, out string dName, out int level)
    {
        dName = string.Empty;
        level = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (TryParseEmbeddedLogLevelJson(text, out dName, out level))
        {
            return true;
        }

        var driverParenMatch = DriverParenPattern.Match(text);
        if (driverParenMatch.Success
            && int.TryParse(driverParenMatch.Groups[1].Value, out var driverId)
            && int.TryParse(driverParenMatch.Groups[2].Value, out var driverLevel))
        {
            dName = $"DRIVER//{driverId}";
            level = driverLevel;
            return true;
        }

        var driverDirectMatch = DriverDirectPattern.Match(text);
        if (driverDirectMatch.Success
            && int.TryParse(driverDirectMatch.Groups[2].Value, out var directLevel))
        {
            dName = driverDirectMatch.Groups[1].Value;
            level = directLevel;
            return true;
        }

        var channelMatch = ChannelPattern.Match(text);
        if (channelMatch.Success
            && int.TryParse(channelMatch.Groups[2].Value, out var channelLevel))
        {
            dName = channelMatch.Groups[1].Value;
            level = channelLevel;
            return true;
        }

        return false;
    }

    private static bool TryParseEmbeddedLogLevelJson(string text, out string dName, out int level)
    {
        dName = string.Empty;
        level = 0;

        var jsonStart = text.IndexOf('{');
        if (jsonStart < 0)
        {
            return false;
        }

        var jsonText = text.Substring(jsonStart);
        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;
            if (!root.TryGetProperty("resource", out var resource) ||
                !string.Equals(resource.GetString(), "LogLevel", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!root.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!value.TryGetProperty("type", out var typeElement))
            {
                return false;
            }

            var type = typeElement.GetString();
            if (string.IsNullOrWhiteSpace(type))
            {
                return false;
            }

            if (!TryReadLevel(value, out var parsedLevel))
            {
                return false;
            }

            dName = type;
            level = parsedLevel;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryReadLevel(JsonElement value, out int level)
    {
        level = 0;
        if (!value.TryGetProperty("level", out var levelElement))
        {
            return false;
        }

        if (levelElement.ValueKind == JsonValueKind.Number && levelElement.TryGetInt32(out level))
        {
            return true;
        }

        if (levelElement.ValueKind == JsonValueKind.String && int.TryParse(levelElement.GetString(), out level))
        {
            return true;
        }

        return false;
    }
}
