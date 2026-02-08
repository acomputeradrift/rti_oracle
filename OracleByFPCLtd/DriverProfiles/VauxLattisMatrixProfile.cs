using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class VauxLattisMatrixProfile
{
    private static readonly Regex CommandPattern = new Regex(
        "Driver - Command:\\s*'(?<command>[^']+)'",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IDriverProfileMapper Mapper { get; } = new VauxLattisMatrixMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "Vaux Lattis Matrix",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        new List<AdditionalInfoSheetSchema>
        {
            new("Vaux Lattis Matrix", new List<AdditionalInfoColumn>
            {
                new("Audio Zone Input Index", AdditionalInfoColumnRole.InputIndex),
                new("Audio Zone Input Name", AdditionalInfoColumnRole.InputName),
                new("Audio Zone Output Index", AdditionalInfoColumnRole.OutputIndex),
                new("Audio Zone Output Name", AdditionalInfoColumnRole.OutputName)
            })
        },
        Mapper);

    private sealed class VauxLattisMatrixMapper : IDriverProfileMapper
    {
        public bool TryMap(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            mappedText = rawText ?? "";
            unresolved = false;
            if (string.IsNullOrWhiteSpace(rawText) || bundle is null)
            {
                return false;
            }

            var match = CommandPattern.Match(rawText);
            if (!match.Success)
            {
                return false;
            }

            var commandText = match.Groups["command"].Value;
            const string prefix = "Vaux Lattis Matrix\\Output Settings\\";
            if (!commandText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var commandBody = commandText.Substring(prefix.Length);
            if (!TryParseCommand(commandBody, out var name, out var args))
            {
                return false;
            }

            if (!bundle.Additional.Drivers.TryGetValue(Definition.DeviceName, out var driverData))
            {
                unresolved = true;
                return true;
            }

            var mappedArgs = args.ToList();
            if (name.Equals("Source Select", StringComparison.OrdinalIgnoreCase))
            {
                if (mappedArgs.Count >= 3)
                {
                    mappedArgs[1] = ResolveOutputName(driverData, mappedArgs[1], ref unresolved);
                    mappedArgs[2] = ResolveInputName(driverData, mappedArgs[2], ref unresolved);
                }
            }
            else if (name.Equals("Output Mute", StringComparison.OrdinalIgnoreCase))
            {
                if (mappedArgs.Count >= 2)
                {
                    mappedArgs[1] = ResolveOutputName(driverData, mappedArgs[1], ref unresolved);
                }
            }
            else if (name.Equals("Output Off", StringComparison.OrdinalIgnoreCase))
            {
                if (mappedArgs.Count >= 1)
                {
                    mappedArgs[0] = ResolveOutputName(driverData, mappedArgs[0], ref unresolved);
                }
            }
            else if (name.Equals("Volume Up", StringComparison.OrdinalIgnoreCase))
            {
                if (mappedArgs.Count >= 1)
                {
                    mappedArgs[0] = ResolveOutputName(driverData, mappedArgs[0], ref unresolved);
                }
            }
            else
            {
                return false;
            }

            var updatedCommandBody = $"{name}({string.Join(", ", mappedArgs)})";
            var updatedCommandText = $"{prefix}{updatedCommandBody}";

            var replacementStart = match.Groups["command"].Index;
            mappedText = rawText.Substring(0, replacementStart)
                + updatedCommandText
                + rawText.Substring(replacementStart + match.Groups["command"].Length);
            return true;
        }

        private static bool TryParseCommand(string commandBody, out string name, out string[] args)
        {
            name = string.Empty;
            args = Array.Empty<string>();
            var openIndex = commandBody.IndexOf('(');
            var closeIndex = commandBody.LastIndexOf(')');
            if (openIndex <= 0 || closeIndex <= openIndex)
            {
                return false;
            }

            name = commandBody.Substring(0, openIndex).Trim();
            var argsText = commandBody.Substring(openIndex + 1, closeIndex - openIndex - 1);
            args = argsText
                .Split(',', StringSplitOptions.TrimEntries)
                .Where(arg => !string.IsNullOrWhiteSpace(arg))
                .ToArray();
            return !string.IsNullOrWhiteSpace(name);
        }

        private static string ResolveOutputName(AdditionalDriverData data, string rawValue, ref bool unresolved)
        {
            if (!TryParseIndex(rawValue, out var index))
            {
                unresolved = true;
                return rawValue;
            }

            if (data.OutputNames.TryGetValue(index, out var name) && !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            unresolved = true;
            return rawValue;
        }

        private static string ResolveInputName(AdditionalDriverData data, string rawValue, ref bool unresolved)
        {
            if (!TryParseIndex(rawValue, out var index))
            {
                unresolved = true;
                return rawValue;
            }

            if (data.InputNames.TryGetValue(index, out var name) && !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            unresolved = true;
            return rawValue;
        }

        private static bool TryParseIndex(string value, out int index)
        {
            index = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
            {
                return true;
            }

            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                return false;
            }

            var rounded = Math.Round(number);
            if (Math.Abs(number - rounded) > 0.0001)
            {
                return false;
            }

            index = (int)rounded;
            return true;
        }
    }
}
