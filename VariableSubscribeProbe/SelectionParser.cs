using System.Globalization;

namespace VariableSubscribeProbe;

public static class SelectionParser
{
    public static IReadOnlyList<int> Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Array.Empty<int>();
        }

        var results = new SortedSet<int>();
        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            if (part.Contains('-', StringComparison.Ordinal))
            {
                var rangeParts = part.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (rangeParts.Length != 2)
                {
                    continue;
                }

                if (!int.TryParse(rangeParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var start))
                {
                    continue;
                }

                if (!int.TryParse(rangeParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var end))
                {
                    continue;
                }

                if (end < start)
                {
                    (start, end) = (end, start);
                }

                for (var i = start; i <= end; i++)
                {
                    results.Add(i);
                }
            }
            else if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                results.Add(value);
            }
        }

        return results.ToList();
    }
}
