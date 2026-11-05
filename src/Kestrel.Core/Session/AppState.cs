using Kestrel.Core.Languages;

namespace Kestrel.Core.Session;

public sealed class AppState
{
    public string Theme { get; set; } = "Dark";
    public bool WordWrap { get; set; }
    public bool ShowWhitespace { get; set; }
    public int Zoom { get; set; }
    public WindowBounds? Window { get; set; }
    public List<SessionFile> OpenFiles { get; set; } = [];
    public int ActiveIndex { get; set; }
}

public sealed class SessionFile
{
    public string Path { get; set; } = "";
    public int CaretPosition { get; set; }
    public int FirstVisibleLine { get; set; }
    public LanguageId? LanguageOverride { get; set; }
}

public sealed record WindowBounds(int X, int Y, int Width, int Height, bool Maximized);
