namespace Kestrel.Core.Languages;

public enum LanguageId { PlainText, Sql, JavaScript, TypeScript, Python, CSharp, Java, Html, Css, Json, Xml, Markdown }

public static class LanguageCatalog
{
    private static readonly Dictionary<string, LanguageId> ByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".sql"] = LanguageId.Sql, [".pgsql"] = LanguageId.Sql, [".psql"] = LanguageId.Sql,
        [".js"] = LanguageId.JavaScript, [".mjs"] = LanguageId.JavaScript, [".cjs"] = LanguageId.JavaScript, [".jsx"] = LanguageId.JavaScript,
        [".ts"] = LanguageId.TypeScript, [".tsx"] = LanguageId.TypeScript, [".mts"] = LanguageId.TypeScript,
        [".py"] = LanguageId.Python, [".pyw"] = LanguageId.Python,
        [".cs"] = LanguageId.CSharp, [".csx"] = LanguageId.CSharp,
        [".java"] = LanguageId.Java,
        [".html"] = LanguageId.Html, [".htm"] = LanguageId.Html, [".xhtml"] = LanguageId.Html,
        [".css"] = LanguageId.Css,
        [".json"] = LanguageId.Json, [".jsonc"] = LanguageId.Json,
        [".xml"] = LanguageId.Xml, [".xsl"] = LanguageId.Xml, [".xslt"] = LanguageId.Xml, [".pcf"] = LanguageId.Xml,
        [".csproj"] = LanguageId.Xml, [".config"] = LanguageId.Xml, [".svg"] = LanguageId.Xml,
        [".md"] = LanguageId.Markdown, [".markdown"] = LanguageId.Markdown,
    };

    public static IReadOnlyList<LanguageId> All { get; } = Enum.GetValues<LanguageId>();

    public static LanguageId FromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return LanguageId.PlainText;
        return ByExtension.TryGetValue(Path.GetExtension(path), out var id) ? id : LanguageId.PlainText;
    }

    public static string DisplayName(LanguageId id) => id switch
    {
        LanguageId.PlainText => "Plain Text",
        LanguageId.Sql => "SQL",
        LanguageId.JavaScript => "JavaScript",
        LanguageId.TypeScript => "TypeScript",
        LanguageId.Python => "Python",
        LanguageId.CSharp => "C#",
        LanguageId.Java => "Java",
        LanguageId.Html => "HTML",
        LanguageId.Css => "CSS",
        LanguageId.Json => "JSON",
        LanguageId.Xml => "XML",
        LanguageId.Markdown => "Markdown",
        _ => id.ToString(),
    };
}
