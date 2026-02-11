using System;
using System.Reflection;

namespace OracleByFPCLtd;

public static class AppVersion
{
    public static string CurrentLabel() => $"v{ResolveVersion(Assembly.GetExecutingAssembly())}";

    private static string ResolveVersion(Assembly assembly)
    {
        var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            var sanitized = info.Split('+')[0].Split('-')[0];
            if (Version.TryParse(sanitized, out var parsed))
            {
                return Format(parsed);
            }

            return sanitized;
        }

        var version = assembly.GetName().Version;
        return version == null ? "unknown" : Format(version);
    }

    private static string Format(Version version)
    {
        if (version.Build <= 0 && version.Revision <= 0)
        {
            return $"{version.Major}.{version.Minor}";
        }

        if (version.Revision <= 0)
        {
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
