using System.Collections.Generic;
using OracleByFPCLtd.DiagnosticsTransport;

namespace OracleByFPCLtd;

public static class DiagnosticsDriverSelector
{
    public static bool TryGetDiagnosticsDriverDName(IEnumerable<DriverInfo> drivers, out string dName)
    {
        dName = string.Empty;
        if (drivers == null)
        {
            return false;
        }

        foreach (var driver in drivers)
        {
            if (driver == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(driver.Name)
                && driver.Name.StartsWith("Diagnostics:", System.StringComparison.OrdinalIgnoreCase))
            {
                dName = driver.DName ?? string.Empty;
                return !string.IsNullOrWhiteSpace(dName);
            }
        }

        return false;
    }
}
