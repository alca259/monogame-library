namespace MonoGame.Editor.WinForms.Dialogs;

/// <summary>
/// Non-modal progress window shown while code generation is in progress.
/// Positioned at the bottom-right corner of the screen.
/// </summary>
public sealed class CodeGenProgressDialog : Form
{
    #region Controls

    private readonly Label       _statusLabel;
    private readonly ProgressBar _progressBar;
    private readonly ListView    _fileList;
    private readonly Button      _closeButton;

    #endregion

    #region Constructor

    /// <summary>Initializes the dialog and positions it at the bottom-right of the primary screen.</summary>
    public CodeGenProgressDialog()
    {
        Text            = "Code Generation";
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition   = FormStartPosition.Manual;
        Size            = new System.Drawing.Size(360, 300);
        Font            = new System.Drawing.Font("Segoe UI", 9f);
        ShowInTaskbar   = false;
        TopMost         = true;

        PositionBottomRight();

        _statusLabel = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 24,
            Text      = "Generating code...",
            Padding   = new Padding(4, 4, 0, 0),
        };

        _progressBar = new ProgressBar
        {
            Dock  = DockStyle.Top,
            Height = 16,
            Style  = ProgressBarStyle.Marquee,
        };

        _fileList = new ListView
        {
            Dock          = DockStyle.Fill,
            View          = View.Details,
            FullRowSelect = true,
            HeaderStyle   = ColumnHeaderStyle.Nonclickable,
            BorderStyle   = BorderStyle.None,
        };
        _fileList.Columns.Add("File", 220);
        _fileList.Columns.Add("Status", 70);

        _closeButton = new Button
        {
            Dock     = DockStyle.Bottom,
            Height   = 28,
            Text     = "Close",
            Enabled  = false,
            FlatStyle = FlatStyle.Flat,
        };
        _closeButton.Click += (_, _) => Close();

        Controls.Add(_fileList);
        Controls.Add(_progressBar);
        Controls.Add(_statusLabel);
        Controls.Add(_closeButton);
    }

    #endregion

    #region Public API

    /// <summary>Adds a file entry with its generation status.</summary>
    public void AddFileResult(string filePath, bool success)
    {
        if (InvokeRequired) { BeginInvoke(() => AddFileResult(filePath, success)); return; }

        ListViewItem item = new(Path.GetFileName(filePath));
        item.SubItems.Add(success ? "OK" : "FAILED");
        item.ForeColor = success
            ? System.Drawing.Color.LightGreen
            : System.Drawing.Color.IndianRed;
        _fileList.Items.Add(item);
    }

    /// <summary>Marks the dialog as complete, enabling the Close button and stopping the progress bar.</summary>
    public void MarkComplete(int successCount, int failedCount)
    {
        if (InvokeRequired) { BeginInvoke(() => MarkComplete(successCount, failedCount)); return; }

        _progressBar.Style = ProgressBarStyle.Blocks;
        _progressBar.Value = 100;
        _statusLabel.Text  = $"Done — {successCount} OK, {failedCount} failed.";
        _closeButton.Enabled = true;
    }

    #endregion

    #region Helpers

    private void PositionBottomRight()
    {
        System.Drawing.Rectangle screen = System.Windows.Forms.Screen.PrimaryScreen?.WorkingArea
            ?? new System.Drawing.Rectangle(0, 0, 1280, 720);

        int x = screen.Right  - Width  - 16;
        int y = screen.Bottom - Height - 16;
        Location = new System.Drawing.Point(x, y);
    }

    #endregion
}
