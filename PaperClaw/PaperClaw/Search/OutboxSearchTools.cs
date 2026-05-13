namespace PaperClaw.Search;

public class OutboxSearchTools
{
    private readonly string _outboxRoot;

    public OutboxSearchTools(string outboxRoot)
    {
        _outboxRoot = Path.GetFullPath(outboxRoot);
    }

    public async Task<string> SearchByText(string keyword)
    {
        var results = new List<string>();
        if (!Directory.Exists(_outboxRoot))
            return "No documents found.";

        foreach (var textFile in Directory.EnumerateFiles(_outboxRoot, "text.txt", SearchOption.AllDirectories))
        {
            if (!IsUnderRoot(textFile)) continue;
            var content = await File.ReadAllTextAsync(textFile);
            if (!content.Contains(keyword, StringComparison.OrdinalIgnoreCase)) continue;

            var docDir = Path.GetDirectoryName(textFile)!;
            var relPath = Path.GetRelativePath(_outboxRoot, docDir);
            var transcriptPath = Path.Combine(docDir, "transcript.md");
            var transcript = File.Exists(transcriptPath)
                ? await File.ReadAllTextAsync(transcriptPath)
                : "(no transcript)";
            results.Add($"### {relPath}\n{transcript}");
        }

        return results.Count == 0 ? "No documents found." : string.Join("\n\n", results);
    }

    public async Task<string> SearchByType(string type)
    {
        var results = new List<string>();
        if (!Directory.Exists(_outboxRoot))
            return "No documents found.";

        // Structure: {year}/{month}/{type}/{id}/
        foreach (var yearDir in Directory.EnumerateDirectories(_outboxRoot))
        {
            if (!IsUnderRoot(yearDir)) continue;
            foreach (var monthDir in Directory.EnumerateDirectories(yearDir))
            {
                if (!IsUnderRoot(monthDir)) continue;
                var typeDir = Path.Combine(monthDir, type);
                if (!Directory.Exists(typeDir)) continue;
                foreach (var idDir in Directory.EnumerateDirectories(typeDir))
                {
                    if (!IsUnderRoot(idDir)) continue;
                    var relPath = Path.GetRelativePath(_outboxRoot, idDir);
                    var transcriptPath = Path.Combine(idDir, "transcript.md");
                    var transcript = File.Exists(transcriptPath)
                        ? await File.ReadAllTextAsync(transcriptPath)
                        : "(no transcript)";
                    results.Add($"### {relPath}\n{transcript}");
                }
            }
        }

        return results.Count == 0 ? "No documents found." : string.Join("\n\n", results);
    }

    public async Task<string> SearchByDateRange(string fromYearMonth, string toYearMonth)
    {
        if (!TryParseYearMonth(fromYearMonth, out var from) || !TryParseYearMonth(toYearMonth, out var to))
            return "Invalid date format. Use YYYY-MM format, e.g. 2024-01.";

        var results = new List<string>();
        if (!Directory.Exists(_outboxRoot))
            return "No documents found.";

        foreach (var yearDir in Directory.EnumerateDirectories(_outboxRoot))
        {
            if (!IsUnderRoot(yearDir)) continue;
            if (!int.TryParse(Path.GetFileName(yearDir), out var year)) continue;
            foreach (var monthDir in Directory.EnumerateDirectories(yearDir))
            {
                if (!IsUnderRoot(monthDir)) continue;
                if (!int.TryParse(Path.GetFileName(monthDir), out var month)) continue;
                var folderDate = new DateOnly(year, month, 1);
                if (folderDate < from || folderDate > to) continue;

                foreach (var typeDir in Directory.EnumerateDirectories(monthDir))
                {
                    if (!IsUnderRoot(typeDir)) continue;
                    foreach (var idDir in Directory.EnumerateDirectories(typeDir))
                    {
                        if (!IsUnderRoot(idDir)) continue;
                        var relPath = Path.GetRelativePath(_outboxRoot, idDir);
                        var transcriptPath = Path.Combine(idDir, "transcript.md");
                        var transcript = File.Exists(transcriptPath)
                            ? await File.ReadAllTextAsync(transcriptPath)
                            : "(no transcript)";
                        results.Add($"### {relPath}\n{transcript}");
                    }
                }
            }
        }

        return results.Count == 0 ? "No documents found." : string.Join("\n\n", results);
    }

    internal bool IsUnderRoot(string path)
    {
        var full = Path.GetFullPath(path);
        return full.StartsWith(_outboxRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || full.Equals(_outboxRoot, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool TryParseYearMonth(string value, out DateOnly result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var parts = value.Split('-');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var month)) return false;
        if (month < 1 || month > 12) return false;
        result = new DateOnly(year, month, 1);
        return true;
    }
}
