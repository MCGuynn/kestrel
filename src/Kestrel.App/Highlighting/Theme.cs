namespace Kestrel.App.Highlighting;

internal enum TokenRole { Comment, Keyword, Keyword2, String, Number, Operator, Preprocessor, Regex, Tag, Attribute, Heading }

internal sealed record Theme(
    string Name,
    Color Back,
    Color Fore,
    Color Caret,
    Color Selection,
    Color CurrentLine,
    Color MarginBack,
    Color MarginFore,
    IReadOnlyDictionary<TokenRole, Color> Tokens)
{
    public static Theme Dark { get; } = new("Dark",
        Hex("#1E1E1E"), Hex("#D4D4D4"), Hex("#AEAFAD"), Hex("#264F78"), Hex("#2A2D2E"), Hex("#1E1E1E"), Hex("#858585"),
        new Dictionary<TokenRole, Color>
        {
            [TokenRole.Comment] = Hex("#6A9955"),
            [TokenRole.Keyword] = Hex("#569CD6"),
            [TokenRole.Keyword2] = Hex("#4EC9B0"),
            [TokenRole.String] = Hex("#CE9178"),
            [TokenRole.Number] = Hex("#B5CEA8"),
            [TokenRole.Operator] = Hex("#D4D4D4"),
            [TokenRole.Preprocessor] = Hex("#C586C0"),
            [TokenRole.Regex] = Hex("#D16969"),
            [TokenRole.Tag] = Hex("#569CD6"),
            [TokenRole.Attribute] = Hex("#9CDCFE"),
            [TokenRole.Heading] = Hex("#DCDCAA"),
        });

    public static Theme Light { get; } = new("Light",
        Hex("#FFFFFF"), Hex("#1F1F1F"), Hex("#000000"), Hex("#ADD6FF"), Hex("#F3F3F3"), Hex("#F5F5F5"), Hex("#8A8A8A"),
        new Dictionary<TokenRole, Color>
        {
            [TokenRole.Comment] = Hex("#008000"),
            [TokenRole.Keyword] = Hex("#0000FF"),
            [TokenRole.Keyword2] = Hex("#267F99"),
            [TokenRole.String] = Hex("#A31515"),
            [TokenRole.Number] = Hex("#098658"),
            [TokenRole.Operator] = Hex("#1F1F1F"),
            [TokenRole.Preprocessor] = Hex("#AF00DB"),
            [TokenRole.Regex] = Hex("#811F3F"),
            [TokenRole.Tag] = Hex("#800000"),
            [TokenRole.Attribute] = Hex("#E50000"),
            [TokenRole.Heading] = Hex("#0451A5"),
        });

    public static Theme ByName(string? name) => name == "Light" ? Light : Dark;

    private static Color Hex(string hex) => ColorTranslator.FromHtml(hex);
}
