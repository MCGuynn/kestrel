using Kestrel.App.Dialogs;
using Kestrel.App.Highlighting;
using Kestrel.Core.Languages;
using Kestrel.Core.Session;
using Kestrel.Core.Text;
using ScintillaNET;

namespace Kestrel.App;

internal sealed class MainForm : Form
{
    private const string AppName = "Kestrel";

    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, ShowToolTips = true };
    private readonly MenuStrip _menu = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _posLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _langLabel = StatusCell();
    private readonly ToolStripStatusLabel _eolLabel = StatusCell();
    private readonly ToolStripStatusLabel _encLabel = StatusCell();
    private readonly string _statePath;
    private readonly AppState _state;
    private Theme _theme;
    private int _untitledCounter;
    private FindReplaceForm? _findForm;
    private ToolStripMenuItem _wrapItem = null!;
    private ToolStripMenuItem _whitespaceItem = null!;
    private ToolStripMenuItem _darkItem = null!;
    private ToolStripMenuItem _lightItem = null!;

    public MainForm(string statePath, IReadOnlyList<string> args)
    {
        _statePath = statePath;
        _state = AppStateStore.Load(statePath);
        _theme = Theme.ByName(_state.Theme);

        Text = AppName;
        Size = new Size(1100, 750);
        AllowDrop = true;
        _tabs.AllowDrop = true;

        BuildMenu();
        _status.Items.AddRange([_posLabel, _langLabel, _eolLabel, _encLabel]);
        Controls.Add(_tabs);
        Controls.Add(_menu);
        Controls.Add(_status);
        MainMenuStrip = _menu;

        _tabs.SelectedIndexChanged += (_, _) => OnActiveDocumentChanged();
        _tabs.MouseUp += OnTabsMouseUp;
        foreach (Control target in new Control[] { this, _tabs })
        {
            target.DragEnter += OnDragEnter;
            target.DragDrop += OnDragDrop;
        }
        FormClosing += OnFormClosing;

        RestoreWindow();
        RestoreSession();
        foreach (string arg in args) OpenPath(arg);
        if (_tabs.TabCount == 0) NewDocument();
        UpdateChrome();
    }

    private EditorDocument? Current => _tabs.SelectedTab?.Tag as EditorDocument;

    private IEnumerable<EditorDocument> Documents => _tabs.TabPages.Cast<TabPage>().Select(p => (EditorDocument)p.Tag!);

    // ---------- Menu ----------

    private void BuildMenu()
    {
        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.AddRange([
            Item("&New", NewDocument, Keys.Control | Keys.N),
            Item("&Open...", OpenWithDialog, Keys.Control | Keys.O),
            new ToolStripSeparator(),
            Item("&Save", () => WithDoc(d => Save(d)), Keys.Control | Keys.S),
            Item("Save &As...", () => WithDoc(d => Save(d, forceDialog: true)), Keys.Control | Keys.Alt | Keys.S),
            Item("Save A&ll", SaveAll, Keys.Control | Keys.Shift | Keys.S),
            new ToolStripSeparator(),
            Item("&Close", () => WithDoc(d => CloseDocument(d)), Keys.Control | Keys.W),
            Item("Close All", CloseAll, Keys.Control | Keys.Shift | Keys.W),
            new ToolStripSeparator(),
            Item("E&xit", Close),
        ]);

        var lineOps = new ToolStripMenuItem("&Line Operations");
        lineOps.DropDownItems.AddRange([
            Item("Duplicate Line", () => Cmd(Command.LineDuplicate), Keys.Control | Keys.D),
            Item("Delete Line", () => Cmd(Command.LineDelete), Keys.Control | Keys.Shift | Keys.L),
            Item("Move Line Up", () => Cmd(Command.MoveSelectedLinesUp), Keys.Control | Keys.Shift | Keys.Up),
            Item("Move Line Down", () => Cmd(Command.MoveSelectedLinesDown), Keys.Control | Keys.Shift | Keys.Down),
            new ToolStripSeparator(),
            Item("Sort Ascending", () => ApplyLineOp(l => LineOps.SortAscending(l))),
            Item("Sort Ascending (Ignore Case)", () => ApplyLineOp(l => LineOps.SortAscending(l, ignoreCase: true))),
            Item("Sort Descending", () => ApplyLineOp(l => LineOps.SortDescending(l))),
            Item("Remove Duplicate Lines", () => ApplyLineOp(l => LineOps.RemoveDuplicates(l))),
            Item("Remove Empty Lines", () => ApplyLineOp(l => LineOps.RemoveEmpty(l))),
            Item("Trim Trailing Whitespace", () => ApplyLineOp(l => LineOps.TrimTrailing(l))),
            Item("Trim Leading Whitespace", () => ApplyLineOp(l => LineOps.TrimLeading(l))),
        ]);

        var caseMenu = new ToolStripMenuItem("C&onvert Case");
        caseMenu.DropDownItems.AddRange([
            Item("UPPERCASE", () => Cmd(Command.Uppercase), Keys.Control | Keys.Shift | Keys.U),
            Item("lowercase", () => Cmd(Command.Lowercase), Keys.Control | Keys.U),
        ]);

        var edit = new ToolStripMenuItem("&Edit");
        edit.DropDownItems.AddRange([
            Item("&Undo", () => WithEditor(s => s.Undo()), Keys.Control | Keys.Z),
            Item("&Redo", () => WithEditor(s => s.Redo()), Keys.Control | Keys.Y),
            new ToolStripSeparator(),
            Item("Cu&t", () => WithEditor(s => s.Cut()), Keys.Control | Keys.X),
            Item("&Copy", () => WithEditor(s => s.Copy()), Keys.Control | Keys.C),
            Item("&Paste", () => WithEditor(s => s.Paste()), Keys.Control | Keys.V),
            Item("Select &All", () => WithEditor(s => s.SelectAll()), Keys.Control | Keys.A),
            new ToolStripSeparator(),
            lineOps,
            caseMenu,
        ]);

        var search = new ToolStripMenuItem("&Search");
        search.DropDownItems.AddRange([
            Item("&Find...", () => ShowFind(focusReplace: false), Keys.Control | Keys.F),
            Item("&Replace...", () => ShowFind(focusReplace: true), Keys.Control | Keys.H),
            Item("Find &Next", () => FindFromMain(forward: true), Keys.F3),
            Item("Find &Previous", () => FindFromMain(forward: false), Keys.Shift | Keys.F3),
            new ToolStripSeparator(),
            Item("&Go to Line...", GoToLine, Keys.Control | Keys.G),
        ]);

        _wrapItem = Item("&Word Wrap", ToggleWrap);
        _wrapItem.Checked = _state.WordWrap;
        _whitespaceItem = Item("Show &Whitespace and Line Endings", ToggleWhitespace);
        _whitespaceItem.Checked = _state.ShowWhitespace;
        _darkItem = Item("&Dark", () => SetTheme(Theme.Dark));
        _lightItem = Item("&Light", () => SetTheme(Theme.Light));
        var theme = new ToolStripMenuItem("&Theme");
        theme.DropDownItems.AddRange([_darkItem, _lightItem]);
        UpdateThemeChecks();

        var view = new ToolStripMenuItem("&View");
        view.DropDownItems.AddRange([
            _wrapItem,
            _whitespaceItem,
            new ToolStripSeparator(),
            Item("Zoom &In", () => SetZoom(_state.Zoom + 1), Keys.Control | Keys.Oemplus, "Ctrl++"),
            Item("Zoom &Out", () => SetZoom(_state.Zoom - 1), Keys.Control | Keys.OemMinus, "Ctrl+-"),
            Item("&Reset Zoom", () => SetZoom(0), Keys.Control | Keys.D0, "Ctrl+0"),
            new ToolStripSeparator(),
            theme,
        ]);

        var convert = new ToolStripMenuItem("&Convert to (on next save)");
        var reopen = new ToolStripMenuItem("&Reopen as");
        foreach (var enc in DocEncoding.All)
        {
            var item = Item(enc.DisplayName, () => ConvertEncoding(enc));
            item.Tag = enc;
            convert.DropDownItems.Add(item);
            reopen.DropDownItems.Add(Item(enc.DisplayName, () => ReopenAs(enc)));
        }
        convert.DropDownOpening += (_, _) =>
        {
            foreach (var i in convert.DropDownItems.OfType<ToolStripMenuItem>())
                i.Checked = i.Tag is DocEncoding e && e == Current?.FileEncoding;
        };
        var encoding = new ToolStripMenuItem("E&ncoding");
        encoding.DropDownItems.AddRange([convert, reopen]);

        var eol = new ToolStripMenuItem("Line &Endings");
        foreach (var mode in new[] { EolMode.CrLf, EolMode.Lf, EolMode.Cr })
        {
            var item = Item(EolLabel(mode), () => WithDoc(d => d.ConvertEol(mode)));
            item.Tag = mode;
            eol.DropDownItems.Add(item);
        }
        eol.DropDownOpening += (_, _) =>
        {
            foreach (var i in eol.DropDownItems.OfType<ToolStripMenuItem>())
                i.Checked = i.Tag is EolMode m && m == Current?.Eol;
        };

        var language = new ToolStripMenuItem("&Language");
        language.DropDownItems.Add(Item("Auto-detect from Extension",
            () => WithDoc(d => d.SetLanguage(LanguageCatalog.FromPath(d.FilePath), overridden: false))));
        language.DropDownItems.Add(new ToolStripSeparator());
        foreach (var id in LanguageCatalog.All)
        {
            var item = Item(LanguageCatalog.DisplayName(id), () => WithDoc(d => d.SetLanguage(id, overridden: true)));
            item.Tag = id;
            language.DropDownItems.Add(item);
        }
        language.DropDownOpening += (_, _) =>
        {
            foreach (var i in language.DropDownItems.OfType<ToolStripMenuItem>())
                i.Checked = i.Tag is LanguageId l && l == Current?.Language;
        };

        _menu.Items.AddRange([file, edit, search, view, encoding, eol, language]);
    }

    private static ToolStripMenuItem Item(string text, Action onClick, Keys keys = Keys.None, string? keyDisplay = null)
    {
        var item = new ToolStripMenuItem(text, null, (_, _) => onClick());
        if (keys != Keys.None) item.ShortcutKeys = keys;
        if (keyDisplay is not null) item.ShortcutKeyDisplayString = keyDisplay;
        return item;
    }

    private static ToolStripStatusLabel StatusCell() => new()
    {
        BorderSides = ToolStripStatusLabelBorderSides.Left,
        BorderStyle = Border3DStyle.Etched,
        Padding = new Padding(6, 0, 6, 0),
    };

    private void WithDoc(Action<EditorDocument> action)
    {
        if (Current is { } doc) action(doc);
    }

    private void WithEditor(Action<Scintilla> action)
    {
        if (Current is { } doc) action(doc.Editor);
    }

    private void Cmd(Command command) => WithEditor(s => s.ExecuteCmd(command));

    // ---------- Documents ----------

    private EditorDocument CreateDocument(bool forFile)
    {
        string name = forFile ? "Untitled" : $"Untitled {++_untitledCounter}";
        var doc = new EditorDocument(name, _theme);
        doc.StateChanged += (_, _) =>
        {
            UpdateTabTitle(doc);
            if (doc == Current) UpdateChrome();
        };
        doc.Editor.UpdateUI += (_, _) =>
        {
            if (doc == Current) UpdatePosition();
        };
        ApplyViewSettings(doc);
        _tabs.TabPages.Add(doc.Page);
        UpdateTabTitle(doc);
        return doc;
    }

    private void NewDocument()
    {
        var doc = CreateDocument(forFile: false);
        _tabs.SelectedTab = doc.Page;
        doc.Editor.Focus();
        UpdateChrome();
    }

    private EditorDocument? FindOpen(string fullPath) =>
        Documents.FirstOrDefault(d => string.Equals(d.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));

    private EditorDocument? OpenPath(string path, bool select = true)
    {
        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            ShowError($"Invalid path: {path}\n\n{ex.Message}");
            return null;
        }
        if (Directory.Exists(full)) return null;

        if (FindOpen(full) is { } existing)
        {
            if (select) _tabs.SelectedTab = existing.Page;
            return existing;
        }

        var docs = Documents.ToList();
        var reuse = docs.Count == 1 && docs[0].IsPristineUntitled ? docs[0] : null;
        var doc = reuse ?? CreateDocument(forFile: true);
        try
        {
            doc.LoadFromFile(full);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError($"Could not open {full}\n\n{ex.Message}");
            if (reuse is null)
            {
                _tabs.TabPages.Remove(doc.Page);
                doc.Page.Dispose();
            }
            return null;
        }
        if (select) _tabs.SelectedTab = doc.Page;
        UpdateChrome();
        return doc;
    }

    private void OpenWithDialog()
    {
        using var dlg = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "All files (*.*)|*.*|Text (*.txt)|*.txt|SQL (*.sql;*.pgsql)|*.sql;*.pgsql|Web (*.html;*.css;*.js;*.ts)|*.html;*.htm;*.css;*.js;*.ts|Data (*.json;*.xml)|*.json;*.xml",
        };
        if (Current?.FilePath is { } current) dlg.InitialDirectory = Path.GetDirectoryName(current);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        foreach (string f in dlg.FileNames) OpenPath(f);
    }

    private bool Save(EditorDocument doc, bool forceDialog = false)
    {
        string? path = doc.FilePath;
        if (path is null || forceDialog)
        {
            using var dlg = new SaveFileDialog { FileName = doc.DisplayName, Filter = "All files (*.*)|*.*", OverwritePrompt = true };
            if (doc.FilePath is not null) dlg.InitialDirectory = Path.GetDirectoryName(doc.FilePath);
            if (dlg.ShowDialog(this) != DialogResult.OK) return false;
            path = dlg.FileName;
        }

        if (!TextFileCodec.CanEncodeLosslessly(doc.Editor.Text, doc.FileEncoding))
        {
            var answer = MessageBox.Show(this,
                $"Some characters can't be represented in {doc.FileEncoding.DisplayName} and will be replaced with '?'.\n\nSave anyway?",
                AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return false;
        }

        try
        {
            doc.SaveTo(path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError($"Could not save {path}\n\n{ex.Message}");
            return false;
        }
    }

    private void SaveAll()
    {
        foreach (var doc in Documents.Where(d => d.IsDirty).ToList())
        {
            _tabs.SelectedTab = doc.Page;
            if (!Save(doc)) break;
        }
    }

    private bool ConfirmClose(EditorDocument doc)
    {
        if (!doc.IsDirty) return true;
        var answer = MessageBox.Show(this, $"Save changes to {doc.DisplayName}?", AppName,
            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        return answer switch
        {
            DialogResult.Yes => Save(doc),
            DialogResult.No => true,
            _ => false,
        };
    }

    private bool CloseDocument(EditorDocument doc)
    {
        if (!ConfirmClose(doc)) return false;
        _tabs.TabPages.Remove(doc.Page);
        doc.Page.Dispose();
        if (_tabs.TabCount == 0) NewDocument();
        return true;
    }

    private void CloseAll()
    {
        foreach (var doc in Documents.ToList())
        {
            if (!CloseDocument(doc)) break;
        }
    }

    // ---------- Line operations ----------

    /// <summary>Applies a transform to the selected lines, or the whole document when nothing is selected, as one undo step.</summary>
    private void ApplyLineOp(Func<string[], string[]> transform)
    {
        var doc = Current;
        if (doc is null) return;
        var s = doc.Editor;

        int firstLine, lastLine;
        if (s.SelectionStart == s.SelectionEnd)
        {
            firstLine = 0;
            lastLine = s.Lines.Count - 1;
        }
        else
        {
            firstLine = s.LineFromPosition(s.SelectionStart);
            lastLine = s.LineFromPosition(s.SelectionEnd);
            if (lastLine > firstLine && s.Lines[lastLine].Position == s.SelectionEnd) lastLine--;
        }

        int start = s.Lines[firstLine].Position;
        int end = s.Lines[lastLine].EndPosition;
        string block = s.GetTextRange(start, end - start);
        bool trailingEol = block.EndsWith('\n') || block.EndsWith('\r');
        string[] lines = LineOps.Split(block);
        if (trailingEol) lines = lines[..^1];

        string result = LineOps.Join(transform(lines), doc.Eol) + (trailingEol ? TextFileCodec.EolString(doc.Eol) : "");
        if (result == block) return;

        s.BeginUndoAction();
        try
        {
            s.TargetStart = start;
            s.TargetEnd = end;
            s.ReplaceTarget(result);
        }
        finally
        {
            s.EndUndoAction();
        }
        s.SetSelection(start + result.Length, start);
    }

    // ---------- Search ----------

    private void ShowFind(bool focusReplace)
    {
        if (_findForm is null)
        {
            _findForm = new FindReplaceForm(() => Current?.Editor) { Owner = this };
            _findForm.Location = new Point(Left + Math.Max(0, (Width - 560) / 2), Top + 120);
        }
        string selected = Current?.Editor.SelectedText ?? "";
        if (selected.Length > 0 && !selected.Contains('\n') && !selected.Contains('\r')) _findForm.SetPattern(selected);
        _findForm.Show();
        _findForm.Activate();
        _findForm.FocusField(focusReplace);
    }

    private void FindFromMain(bool forward)
    {
        if (_findForm is null || !_findForm.HasPattern)
        {
            ShowFind(focusReplace: false);
            return;
        }
        if (!_findForm.Find(forward)) System.Media.SystemSounds.Beep.Play();
    }

    private void GoToLine()
    {
        var s = Current?.Editor;
        if (s is null) return;
        using var dlg = new GoToLineForm(s.CurrentLine + 1, s.Lines.Count);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var line = s.Lines[dlg.SelectedLine - 1];
        line.EnsureVisible();
        line.Goto();
        s.Focus();
    }

    // ---------- Encoding ----------

    private void ConvertEncoding(DocEncoding encoding)
    {
        var doc = Current;
        if (doc is null || doc.FileEncoding == encoding) return;
        if (!TextFileCodec.CanEncodeLosslessly(doc.Editor.Text, encoding))
        {
            var answer = MessageBox.Show(this,
                $"Some characters can't be represented in {encoding.DisplayName} and will become '?' when saved.\n\nConvert anyway?",
                AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;
        }
        doc.SetEncoding(encoding);
    }

    private void ReopenAs(DocEncoding encoding)
    {
        var doc = Current;
        if (doc?.FilePath is not { } path)
        {
            MessageBox.Show(this, "Save the file first. Reopening reads it from disk.", AppName);
            return;
        }
        if (doc.IsDirty)
        {
            var answer = MessageBox.Show(this,
                $"Discard unsaved changes to {doc.DisplayName} and reopen it as {encoding.DisplayName}?",
                AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;
        }
        try
        {
            doc.LoadFromFile(path, encoding);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError($"Could not reopen {path}\n\n{ex.Message}");
        }
    }

    // ---------- View ----------

    private void ApplyViewSettings(EditorDocument doc)
    {
        var s = doc.Editor;
        s.WrapMode = _state.WordWrap ? WrapMode.Word : WrapMode.None;
        s.ViewWhitespace = _state.ShowWhitespace ? WhitespaceMode.VisibleAlways : WhitespaceMode.Invisible;
        s.ViewEol = _state.ShowWhitespace;
        s.Zoom = _state.Zoom;
    }

    private void ApplyViewSettingsToAll()
    {
        foreach (var doc in Documents) ApplyViewSettings(doc);
    }

    private void ToggleWrap()
    {
        _state.WordWrap = !_state.WordWrap;
        _wrapItem.Checked = _state.WordWrap;
        ApplyViewSettingsToAll();
    }

    private void ToggleWhitespace()
    {
        _state.ShowWhitespace = !_state.ShowWhitespace;
        _whitespaceItem.Checked = _state.ShowWhitespace;
        ApplyViewSettingsToAll();
    }

    private void SetZoom(int zoom)
    {
        _state.Zoom = Math.Clamp(zoom, -10, 20);
        ApplyViewSettingsToAll();
    }

    private void SetTheme(Theme theme)
    {
        _theme = theme;
        _state.Theme = theme.Name;
        foreach (var doc in Documents) doc.ApplyTheme(theme);
        UpdateThemeChecks();
    }

    private void UpdateThemeChecks()
    {
        _darkItem.Checked = _theme.Name == Theme.Dark.Name;
        _lightItem.Checked = _theme.Name == Theme.Light.Name;
    }

    // ---------- Chrome ----------

    private void OnActiveDocumentChanged()
    {
        UpdateChrome();
        Current?.Editor.Focus();
    }

    private void UpdateTabTitle(EditorDocument doc)
    {
        doc.Page.Text = doc.DisplayName + (doc.IsDirty ? " *" : "");
        doc.Page.ToolTipText = doc.FilePath ?? "";
    }

    private void UpdateChrome()
    {
        var doc = Current;
        if (doc is null)
        {
            Text = AppName;
            return;
        }
        Text = $"{doc.DisplayName}{(doc.IsDirty ? " *" : "")} - {AppName}";
        _langLabel.Text = LanguageCatalog.DisplayName(doc.Language);
        _eolLabel.Text = EolLabel(doc.Eol);
        _encLabel.Text = doc.FileEncoding.DisplayName;
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        var s = Current?.Editor;
        if (s is null) return;
        int pos = s.CurrentPosition;
        int line = s.LineFromPosition(pos);
        int col = s.GetColumn(pos);
        int sel = Math.Abs(s.SelectionEnd - s.SelectionStart);
        _posLabel.Text = $"Ln {line + 1}, Col {col + 1}    Sel {sel}    Length {s.TextLength}";
    }

    private static string EolLabel(EolMode mode) => mode switch
    {
        EolMode.Lf => "Unix (LF)",
        EolMode.Cr => "Classic Mac (CR)",
        _ => "Windows (CR LF)",
    };

    private void OnTabsMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Middle) return;
        for (int i = 0; i < _tabs.TabCount; i++)
        {
            if (!_tabs.GetTabRect(i).Contains(e.Location)) continue;
            CloseDocument((EditorDocument)_tabs.TabPages[i].Tag!);
            return;
        }
    }

    private void OnDragEnter(object? sender, DragEventArgs e) =>
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files) return;
        foreach (string f in files) OpenPath(f);
    }

    private void ShowError(string message) =>
        MessageBox.Show(this, message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);

    // ---------- Session ----------

    private void RestoreWindow()
    {
        if (_state.Window is not { } w)
        {
            StartPosition = FormStartPosition.CenterScreen;
            return;
        }
        var rect = new Rectangle(w.X, w.Y, Math.Max(400, w.Width), Math.Max(300, w.Height));
        if (Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(rect)))
        {
            StartPosition = FormStartPosition.Manual;
            Bounds = rect;
        }
        else
        {
            StartPosition = FormStartPosition.CenterScreen;
        }
        if (w.Maximized) WindowState = FormWindowState.Maximized;
    }

    private void RestoreSession()
    {
        foreach (var f in _state.OpenFiles)
        {
            if (!File.Exists(f.Path)) continue;
            var doc = OpenPath(f.Path, select: false);
            if (doc is null) continue;
            if (f.LanguageOverride is { } lang) doc.SetLanguage(lang, overridden: true);
            doc.RestoreView(f.CaretPosition, f.FirstVisibleLine);
        }
        var active = _state.OpenFiles.ElementAtOrDefault(_state.ActiveIndex);
        if (active is not null && FindOpen(Path.GetFullPath(active.Path)) is { } activeDoc) _tabs.SelectedTab = activeDoc.Page;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        foreach (var doc in Documents.ToList())
        {
            if (!doc.IsDirty) continue;
            _tabs.SelectedTab = doc.Page;
            if (!ConfirmClose(doc))
            {
                e.Cancel = true;
                return;
            }
        }
        SaveState();
    }

    private void SaveState()
    {
        var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        _state.Window = new WindowBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height, WindowState == FormWindowState.Maximized);
        _state.Theme = _theme.Name;

        var saved = Documents.Where(d => d.FilePath is not null).ToList();
        _state.OpenFiles = saved.Select(d => new SessionFile
        {
            Path = d.FilePath!,
            CaretPosition = d.Editor.CurrentPosition,
            FirstVisibleLine = d.Editor.FirstVisibleLine,
            LanguageOverride = d.LanguageOverridden ? d.Language : null,
        }).ToList();
        _state.ActiveIndex = Current is { } cur ? Math.Max(0, saved.IndexOf(cur)) : 0;

        try
        {
            AppStateStore.Save(_statePath, _state);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Losing session state is not worth blocking exit.
            System.Diagnostics.Debug.WriteLine($"Kestrel: could not save state: {ex.Message}");
        }
    }
}
