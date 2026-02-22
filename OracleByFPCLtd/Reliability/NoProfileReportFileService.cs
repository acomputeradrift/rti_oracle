using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace OracleByFPCLtd.Reliability;

public sealed record NoProfileReportWriteResult(bool Success, string? Path, string? Error);

public static class NoProfileReportFileService
{
    public static NoProfileReportWriteResult Write(
        object report,
        string filePrefix = "oracle_no_profile_messages",
        IEnumerable<string>? preferredFolders = null,
        Func<DateTime>? localNow = null)
    {
        if (report is null)
        {
            return new NoProfileReportWriteResult(false, null, "Report is null.");
        }

        if (string.IsNullOrWhiteSpace(filePrefix))
        {
            return new NoProfileReportWriteResult(false, null, "File prefix is empty.");
        }

        var now = localNow ?? (() => DateTime.Now);
        var folders = BuildFolderCandidates(preferredFolders).ToList();
        if (folders.Count == 0)
        {
            return new NoProfileReportWriteResult(false, null, "No writable folder candidates were found.");
        }

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        var errors = new List<string>();
        foreach (var folder in folders)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                continue;
            }

            try
            {
                Directory.CreateDirectory(folder);
                var fileName = $"{filePrefix}_{now():yyyyMMdd_HHmmss}.json";
                var path = Path.Combine(folder, fileName);
                File.WriteAllText(path, json);
                return new NoProfileReportWriteResult(true, path, null);
            }
            catch (Exception ex)
            {
                errors.Add($"{folder}: {ex.Message}");
            }
        }

        var error = errors.Count == 0
            ? "No candidate folders could be written."
            : string.Join(" | ", errors);
        return new NoProfileReportWriteResult(false, null, error);
    }

    private static IEnumerable<string> BuildFolderCandidates(IEnumerable<string>? preferredFolders)
    {
        if (preferredFolders != null)
        {
            foreach (var folder in preferredFolders)
            {
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    yield return folder;
                }
            }
            yield break;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (!string.IsNullOrWhiteSpace(desktop) && seen.Add(desktop))
        {
            yield return desktop;
        }

        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrWhiteSpace(documents) && seen.Add(documents))
        {
            yield return documents;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            var appFallback = Path.Combine(localAppData, "Oracle by FP&C", "Reports");
            if (seen.Add(appFallback))
            {
                yield return appFallback;
            }
        }
    }
}
