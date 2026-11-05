using System.Diagnostics.CodeAnalysis;
using Kestrel.App.Search;
using ScintillaNET;

namespace Kestrel.App.Dialogs;

/// <summary>Modeless find/replace window. Hidden rather than disposed on close so options persist.</summary>
internal sealed class FindReplaceForm : Form
{
    private readonly Func<Scintilla?> _editor;
    private readonly TextBox _find = new() { Width = 320 };
    private readonly TextBox _replace = new() { Width = 320 };
    private readonly CheckBox _matchCase = new() { Text = "Match case", AutoSize = true };
    private readonly CheckBox _wholeWord = new() { Text = "Whole word", AutoSize = true };
    private readonly CheckBox _regex = new() { Text = "Regular expression", AutoSize = true };
    private readonly CheckBox _wrap = new() { Text = "Wrap around", AutoSize = true, Checked = true };
    private readonly Label _status = new() { AutoSize = true, ForeColor = SystemColors.GrayText };

    public FindReplaceForm(Func<Scintilla?> editor)
    {
        _editor = editor;
        Text = "Find / Replace";
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(8);

        var findNext = MakeButton("Find Next", () => Find(forward: true));
        var close = MakeButton("Close", Hide);

        var options = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        options.Controls.AddRange([_matchCase, _wholeWord, _regex, _wrap]);

        var grid = new TableLayoutPanel { AutoSize = true, ColumnCount = 3, Dock = DockStyle.Fill };
        grid.Controls.Add(MakeLabel("Find:"), 0, 0);
        grid.Controls.Add(_find, 1, 0);
        grid.Controls.Add(findNext, 2, 0);
        grid.Controls.Add(MakeLabel("Replace:"), 0, 1);
        grid.Controls.Add(_replace, 1, 1);
        grid.Controls.Add(MakeButton("Find Previous", () => Find(forward: false)), 2, 1);
        grid.Controls.Add(options, 1, 2);
        grid.Controls.Add(MakeButton("Replace", Replace), 2, 2);
        grid.Controls.Add(_status, 1, 3);
        grid.Controls.Add(MakeButton("Replace All", ReplaceAll), 2, 3);
        grid.Controls.Add(MakeButton("Count", Count), 2, 4);
        grid.Controls.Add(close, 2, 5);
        Controls.Add(grid);

        AcceptButton = findNext;
        CancelButton = close;
        _regex.CheckedChanged += (_, _) => _wholeWord.Enabled = !_regex.Checked;
        FormClosing += (_, e) =>
        {
            if (e.CloseReason != CloseReason.UserClosing) return;
            e.Cancel = true;
            Hide();
        };
    }

    public bool HasPattern => _find.TextLength > 0;

    public void SetPattern(string text) => _find.Text = text;

    public void FocusField(bool replace)
    {
        var box = replace ? _replace : _find;
        box.Focus();
        box.SelectAll();
    }

    public bool Find(bool forward)
    {
        if (!TryGetContext(out var editor, out var options)) return false;
        bool found = SearchEngine.FindNext(editor, options, forward);
        _status.Text = found ? "" : $"Not found: {options.Pattern}";
        return found;
    }

    private void Replace()
    {
        if (!TryGetContext(out var editor, out var options)) return;
        bool more = SearchEngine.ReplaceCurrent(editor, options);
        _status.Text = more ? "" : "No more matches.";
    }

    private void ReplaceAll()
    {
        if (!TryGetContext(out var editor, out var options)) return;
        int n = SearchEngine.ReplaceAll(editor, options);
        _status.Text = $"Replaced {n} occurrence(s).";
    }

    private void Count()
    {
        if (!TryGetContext(out var editor, out var options)) return;
        _status.Text = $"{SearchEngine.Count(editor, options)} match(es).";
    }

    private bool TryGetContext([NotNullWhen(true)] out Scintilla? editor, [NotNullWhen(true)] out SearchOptions? options)
    {
        editor = _editor();
        options = new SearchOptions(_find.Text, _replace.Text, _matchCase.Checked, _wholeWord.Checked, _regex.Checked, _wrap.Checked);
        if (editor is null)
        {
            _status.Text = "No document is open.";
            options = null;
            return false;
        }
        string? error = SearchEngine.Validate(options);
        if (error is not null)
        {
            _status.Text = error;
            options = null;
            return false;
        }
        _status.Text = "";
        return true;
    }

    private static Button MakeButton(string text, Action onClick)
    {
        var button = new Button { Text = text, Width = 120, Anchor = AnchorStyles.Left | AnchorStyles.Right };
        button.Click += (_, _) => onClick();
        return button;
    }

    private static Label MakeLabel(string text) => new() { Text = text, AutoSize = true, Anchor = AnchorStyles.Left };
}
