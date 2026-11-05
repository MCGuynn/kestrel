namespace Kestrel.Core.Text;

/// <summary>Pure line transforms. The editor extracts lines, calls one of these, and writes the result back as one undo step.</summary>
public static class LineOps
{
    private static readonly string[] Separators = ["\r\n", "\n", "\r"];

    public static string[] Split(string text) => text.Split(Separators, StringSplitOptions.None);

    public static string Join(IEnumerable<string> lines, EolMode eol) => string.Join(TextFileCodec.EolString(eol), lines);

    public static string[] SortAscending(IEnumerable<string> lines, bool ignoreCase = false) =>
        lines.Order(ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).ToArray();

    public static string[] SortDescending(IEnumerable<string> lines, bool ignoreCase = false) =>
        lines.OrderDescending(ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal).ToArray();

    /// <summary>Keeps the first occurrence of each line and preserves order.</summary>
    public static string[] RemoveDuplicates(IEnumerable<string> lines)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var line in lines)
        {
            if (seen.Add(line)) result.Add(line);
        }
        return result.ToArray();
    }

    public static string[] RemoveEmpty(IEnumerable<string> lines) =>
        lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

    public static string[] TrimTrailing(IEnumerable<string> lines) =>
        lines.Select(l => l.TrimEnd(' ', '\t')).ToArray();

    public static string[] TrimLeading(IEnumerable<string> lines) =>
        lines.Select(l => l.TrimStart(' ', '\t')).ToArray();
}
