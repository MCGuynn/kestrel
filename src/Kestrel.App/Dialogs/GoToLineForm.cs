namespace Kestrel.App.Dialogs;

internal sealed class GoToLineForm : Form
{
    private readonly NumericUpDown _line;

    public int SelectedLine => (int)_line.Value;

    public GoToLineForm(int currentLine, int lineCount)
    {
        int max = Math.Max(1, lineCount);
        Text = "Go to Line";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(10);

        _line = new NumericUpDown { Minimum = 1, Maximum = max, Value = Math.Clamp(currentLine, 1, max), Width = 120 };
        var ok = new Button { Text = "Go", DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        var label = new Label { Text = $"Line (1-{max}):", AutoSize = true, Margin = new Padding(0, 6, 6, 0) };

        var panel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
        panel.Controls.AddRange([label, _line, ok, cancel]);
        Controls.Add(panel);
        AcceptButton = ok;
        CancelButton = cancel;
        Shown += (_, _) =>
        {
            _line.Focus();
            _line.Select(0, _line.Text.Length);
        };
    }
}
