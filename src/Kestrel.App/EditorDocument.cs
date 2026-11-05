using System.Globalization;
using Kestrel.App.Highlighting;
using Kestrel.Core.Languages;
using Kestrel.Core.Text;
using ScintillaNET;

namespace Kestrel.App;

/// <summary>One open tab: the Scintilla control plus the file metadata that Scintilla doesn't track.</summary>
internal sealed class EditorDocument
{
    private readonly string _untitledName;
    private Theme _theme;
    private bool _metaDirty;
    private int _lineDigits;

    public Scintilla Editor { get; }
    public TabPage Page { get; }
    public string? FilePath { get; private set; }
    public DocEncoding FileEncoding { get; private set; } = DocEncoding.Utf8;
    public EolMode Eol { get; private set; } = EolMode.CrLf;
    public LanguageId Language { get; private set; } = LanguageId.PlainText;
    public bool LanguageOverridden { get; private set; }

    public bool IsDirty => Editor.Modified || _metaDirty;
    public bool IsPristineUntitled => FilePath is null && !IsDirty && Editor.TextLength == 0;
    public string DisplayName => FilePath is null ? _untitledName : Path.GetFileName(FilePath);

    public event EventHandler? StateChanged;

    public EditorDocument(string untitledName, Theme theme)
    {
        _untitledName = untitledName;
        _theme = theme;
        Editor = new Scintilla { Dock = DockStyle.Fill };
        Page = new TabPage(untitledName) { Tag = this };
        Page.Controls.Add(Editor);
        Configure();
        LanguageStyler.Apply(Editor, Language, _theme);
        UpdateLineNumberWidth(force: true);

        Editor.SavePointLeft += (_, _) => RaiseStateChanged();
        Editor.SavePointReached += (_, _) => RaiseStateChanged();
        Editor.TextChanged += (_, _) => UpdateLineNumberWidth(force: false);
    }

    private void Configure()
    {
        var s = Editor;
        s.EolMode = ToScintilla(Eol);
        s.TabWidth = 4;
        s.IndentationGuides = IndentView.LookBoth;

        // Multi-cursor: Ctrl+click adds a caret; Alt+drag makes a column selection.
        s.MultipleSelection = true;
        s.AdditionalSelectionTyping = true;
        s.MultiPaste = MultiPaste.Each;
        s.VirtualSpaceOptions = VirtualSpace.RectangularSelection;

        s.Margins[0].Type = MarginType.Number;
        s.Margins[1].Width = 0;
        var fold = s.Margins[2];
        fold.Type = MarginType.Symbol;
        fold.Mask = Marker.MaskFolders;
        fold.Sensitive = true;
        fold.Width = 16;

        s.Markers[Marker.Folder].Symbol = MarkerSymbol.BoxPlus;
        s.Markers[Marker.FolderOpen].Symbol = MarkerSymbol.BoxMinus;
        s.Markers[Marker.FolderEnd].Symbol = MarkerSymbol.BoxPlusConnected;
        s.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
        s.Markers[Marker.FolderOpenMid].Symbol = MarkerSymbol.BoxMinusConnected;
        s.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
        s.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;
        s.AutomaticFold = AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change;
    }

    public void LoadFromFile(string path, DocEncoding? forcedEncoding = null)
    {
        string full = Path.GetFullPath(path);
        var (text, encoding, eol) = TextFileCodec.ReadFile(full, forcedEncoding);
        Editor.Text = text;
        Editor.EmptyUndoBuffer();
        Editor.SetSavePoint();
        FilePath = full;
        FileEncoding = encoding;
        Eol = eol;
        Editor.EolMode = ToScintilla(eol);
        _metaDirty = false;
        if (!LanguageOverridden) SetLanguage(LanguageCatalog.FromPath(full), overridden: false);
        RaiseStateChanged();
    }

    public void SaveTo(string path)
    {
        string full = Path.GetFullPath(path);
        TextFileCodec.WriteFileAtomic(full, Editor.Text, FileEncoding);
        bool pathChanged = !string.Equals(full, FilePath, StringComparison.OrdinalIgnoreCase);
        FilePath = full;
        Editor.SetSavePoint();
        _metaDirty = false;
        if (pathChanged && !LanguageOverridden) SetLanguage(LanguageCatalog.FromPath(full), overridden: false);
        RaiseStateChanged();
    }

    public void SetLanguage(LanguageId language, bool overridden)
    {
        Language = language;
        LanguageOverridden = overridden;
        LanguageStyler.Apply(Editor, language, _theme);
        UpdateLineNumberWidth(force: true);
        RaiseStateChanged();
    }

    public void ApplyTheme(Theme theme)
    {
        _theme = theme;
        LanguageStyler.Apply(Editor, Language, theme);
        UpdateLineNumberWidth(force: true);
    }

    /// <summary>Changes the encoding used on the next save. The text itself is unchanged.</summary>
    public void SetEncoding(DocEncoding encoding)
    {
        if (encoding == FileEncoding) return;
        FileEncoding = encoding;
        _metaDirty = true;
        RaiseStateChanged();
    }

    public void ConvertEol(EolMode mode)
    {
        Editor.ConvertEols(ToScintilla(mode));
        Editor.EolMode = ToScintilla(mode);
        Eol = mode;
        RaiseStateChanged();
    }

    public void RestoreView(int caretPosition, int firstVisibleLine)
    {
        Editor.GotoPosition(Math.Clamp(caretPosition, 0, Editor.TextLength));
        Editor.FirstVisibleLine = Math.Clamp(firstVisibleLine, 0, Math.Max(0, Editor.Lines.Count - 1));
    }

    private void UpdateLineNumberWidth(bool force)
    {
        int digits = Editor.Lines.Count.ToString(CultureInfo.InvariantCulture).Length;
        if (!force && digits == _lineDigits) return;
        _lineDigits = digits;
        Editor.Margins[0].Width = Editor.TextWidth(Style.LineNumber, new string('9', digits + 1)) + 4;
    }

    private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    private static Eol ToScintilla(EolMode mode) => mode switch
    {
        EolMode.Lf => ScintillaNET.Eol.Lf,
        EolMode.Cr => ScintillaNET.Eol.Cr,
        _ => ScintillaNET.Eol.CrLf,
    };
}
