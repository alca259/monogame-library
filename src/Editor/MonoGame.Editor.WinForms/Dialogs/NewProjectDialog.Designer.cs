namespace MonoGame.Editor.WinForms.Dialogs;

partial class NewProjectDialog
{
    private System.ComponentModel.IContainer components = null;

    // Basic fields
    private System.Windows.Forms.Label _nameLabel;
    private System.Windows.Forms.TextBox _nameTextBox;
    private System.Windows.Forms.Label _locationLabel;
    private System.Windows.Forms.TextBox _locationTextBox;
    private System.Windows.Forms.Button _browseButton;
    private System.Windows.Forms.Label _previewLabel;
    private System.Windows.Forms.Label _previewValueLabel;

    // Game .csproj
    private System.Windows.Forms.Label _csprojLabel;
    private System.Windows.Forms.TextBox _csprojTextBox;
    private System.Windows.Forms.Button _browseCsprojButton;

    // Content folder
    private System.Windows.Forms.Label _contentLabel;
    private System.Windows.Forms.TextBox _contentTextBox;
    private System.Windows.Forms.Button _browseContentButton;

    // Localization folder
    private System.Windows.Forms.Label _localizationLabel;
    private System.Windows.Forms.TextBox _localizationTextBox;
    private System.Windows.Forms.Button _browseLocalizationButton;

    // Buttons
    private System.Windows.Forms.Button _okButton;
    private System.Windows.Forms.Button _cancelButton;

    // Layout
    private System.Windows.Forms.TableLayoutPanel _gridPanel;
    private System.Windows.Forms.TableLayoutPanel _locationRow;
    private System.Windows.Forms.TableLayoutPanel _csprojRow;
    private System.Windows.Forms.TableLayoutPanel _contentRow;
    private System.Windows.Forms.TableLayoutPanel _localizationRow;
    private System.Windows.Forms.FlowLayoutPanel _buttonPanel;
    private System.Windows.Forms.Label _separatorLabel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        _nameLabel           = new System.Windows.Forms.Label();
        _nameTextBox         = new System.Windows.Forms.TextBox();
        _locationLabel       = new System.Windows.Forms.Label();
        _locationTextBox     = new System.Windows.Forms.TextBox();
        _browseButton        = new System.Windows.Forms.Button();
        _previewLabel        = new System.Windows.Forms.Label();
        _previewValueLabel   = new System.Windows.Forms.Label();
        _separatorLabel      = new System.Windows.Forms.Label();
        _csprojLabel         = new System.Windows.Forms.Label();
        _csprojTextBox       = new System.Windows.Forms.TextBox();
        _browseCsprojButton  = new System.Windows.Forms.Button();
        _contentLabel        = new System.Windows.Forms.Label();
        _contentTextBox      = new System.Windows.Forms.TextBox();
        _browseContentButton = new System.Windows.Forms.Button();
        _localizationLabel        = new System.Windows.Forms.Label();
        _localizationTextBox      = new System.Windows.Forms.TextBox();
        _browseLocalizationButton = new System.Windows.Forms.Button();
        _okButton            = new System.Windows.Forms.Button();
        _cancelButton        = new System.Windows.Forms.Button();
        _gridPanel           = new System.Windows.Forms.TableLayoutPanel();
        _locationRow         = new System.Windows.Forms.TableLayoutPanel();
        _csprojRow           = new System.Windows.Forms.TableLayoutPanel();
        _contentRow          = new System.Windows.Forms.TableLayoutPanel();
        _localizationRow     = new System.Windows.Forms.TableLayoutPanel();
        _buttonPanel         = new System.Windows.Forms.FlowLayoutPanel();

        _gridPanel.SuspendLayout();
        _locationRow.SuspendLayout();
        _csprojRow.SuspendLayout();
        _contentRow.SuspendLayout();
        _localizationRow.SuspendLayout();
        _buttonPanel.SuspendLayout();
        SuspendLayout();

        // ── _nameLabel ────────────────────────────────────────────────────────────
        _nameLabel.AutoSize  = true;
        _nameLabel.Dock      = System.Windows.Forms.DockStyle.Fill;
        _nameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _nameLabel.Text      = "Project Name:";

        // ── _nameTextBox ──────────────────────────────────────────────────────────
        _nameTextBox.Dock = System.Windows.Forms.DockStyle.Fill;

        // ── _locationLabel ────────────────────────────────────────────────────────
        _locationLabel.AutoSize  = true;
        _locationLabel.Dock      = System.Windows.Forms.DockStyle.Fill;
        _locationLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _locationLabel.Text      = "Location:";

        // ── _locationTextBox ──────────────────────────────────────────────────────
        _locationTextBox.Dock     = System.Windows.Forms.DockStyle.Fill;
        _locationTextBox.ReadOnly = true;

        // ── _browseButton ─────────────────────────────────────────────────────────
        _browseButton.AutoSize = true;
        _browseButton.Text     = "Browse...";
        _browseButton.Dock     = System.Windows.Forms.DockStyle.Right;

        // ── _locationRow ──────────────────────────────────────────────────────────
        _locationRow.ColumnCount = 2;
        _locationRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _locationRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
        _locationRow.Controls.Add(_locationTextBox, 0, 0);
        _locationRow.Controls.Add(_browseButton, 1, 0);
        _locationRow.Dock     = System.Windows.Forms.DockStyle.Fill;
        _locationRow.RowCount = 1;
        _locationRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

        // ── _previewLabel ─────────────────────────────────────────────────────────
        _previewLabel.AutoSize  = true;
        _previewLabel.Dock      = System.Windows.Forms.DockStyle.Fill;
        _previewLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _previewLabel.Text      = "Full path:";

        // ── _previewValueLabel ────────────────────────────────────────────────────
        _previewValueLabel.AutoSize  = true;
        _previewValueLabel.Dock      = System.Windows.Forms.DockStyle.Fill;
        _previewValueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _previewValueLabel.ForeColor = System.Drawing.SystemColors.GrayText;

        // ── _separatorLabel ───────────────────────────────────────────────────────
        _separatorLabel.Dock      = System.Windows.Forms.DockStyle.Fill;
        _separatorLabel.Height    = 1;
        _separatorLabel.BackColor = System.Drawing.SystemColors.ControlDark;
        _separatorLabel.Text      = string.Empty;

        // ── _csprojLabel ──────────────────────────────────────────────────────────
        _csprojLabel.AutoSize  = true;
        _csprojLabel.Dock      = System.Windows.Forms.DockStyle.Fill;
        _csprojLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _csprojLabel.Text      = "Game .csproj:";

        // ── _csprojTextBox ────────────────────────────────────────────────────────
        _csprojTextBox.Dock     = System.Windows.Forms.DockStyle.Fill;
        _csprojTextBox.ReadOnly = true;

        // ── _browseCsprojButton ───────────────────────────────────────────────────
        _browseCsprojButton.AutoSize = true;
        _browseCsprojButton.Text     = "Browse...";
        _browseCsprojButton.Dock     = System.Windows.Forms.DockStyle.Right;

        // ── _csprojRow ────────────────────────────────────────────────────────────
        _csprojRow.ColumnCount = 2;
        _csprojRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _csprojRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
        _csprojRow.Controls.Add(_csprojTextBox, 0, 0);
        _csprojRow.Controls.Add(_browseCsprojButton, 1, 0);
        _csprojRow.Dock     = System.Windows.Forms.DockStyle.Fill;
        _csprojRow.RowCount = 1;
        _csprojRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

        // ── _contentLabel ─────────────────────────────────────────────────────────
        _contentLabel.AutoSize  = true;
        _contentLabel.Dock      = System.Windows.Forms.DockStyle.Fill;
        _contentLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _contentLabel.Text      = "Content folder:";

        // ── _contentTextBox ───────────────────────────────────────────────────────
        _contentTextBox.Dock     = System.Windows.Forms.DockStyle.Fill;
        _contentTextBox.ReadOnly = true;

        // ── _browseContentButton ──────────────────────────────────────────────────
        _browseContentButton.AutoSize = true;
        _browseContentButton.Text     = "Browse...";
        _browseContentButton.Dock     = System.Windows.Forms.DockStyle.Right;

        // ── _contentRow ───────────────────────────────────────────────────────────
        _contentRow.ColumnCount = 2;
        _contentRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _contentRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
        _contentRow.Controls.Add(_contentTextBox, 0, 0);
        _contentRow.Controls.Add(_browseContentButton, 1, 0);
        _contentRow.Dock     = System.Windows.Forms.DockStyle.Fill;
        _contentRow.RowCount = 1;
        _contentRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

        // ── _localizationLabel ────────────────────────────────────────────────────
        _localizationLabel.AutoSize  = true;
        _localizationLabel.Dock      = System.Windows.Forms.DockStyle.Fill;
        _localizationLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        _localizationLabel.Text      = "Localization folder:";

        // ── _localizationTextBox ──────────────────────────────────────────────────
        _localizationTextBox.Dock     = System.Windows.Forms.DockStyle.Fill;
        _localizationTextBox.ReadOnly = true;

        // ── _browseLocalizationButton ─────────────────────────────────────────────
        _browseLocalizationButton.AutoSize = true;
        _browseLocalizationButton.Text     = "Browse...";
        _browseLocalizationButton.Dock     = System.Windows.Forms.DockStyle.Right;

        // ── _localizationRow ──────────────────────────────────────────────────────
        _localizationRow.ColumnCount = 2;
        _localizationRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _localizationRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
        _localizationRow.Controls.Add(_localizationTextBox, 0, 0);
        _localizationRow.Controls.Add(_browseLocalizationButton, 1, 0);
        _localizationRow.Dock     = System.Windows.Forms.DockStyle.Fill;
        _localizationRow.RowCount = 1;
        _localizationRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

        // ── _okButton ─────────────────────────────────────────────────────────────
        _okButton.Text    = "OK";
        _okButton.Width   = 80;
        _okButton.Enabled = false;

        // ── _cancelButton ─────────────────────────────────────────────────────────
        _cancelButton.Text         = "Cancel";
        _cancelButton.Width        = 80;
        _cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;

        // ── _buttonPanel ──────────────────────────────────────────────────────────
        _buttonPanel.Controls.Add(_cancelButton);
        _buttonPanel.Controls.Add(_okButton);
        _buttonPanel.Dock          = System.Windows.Forms.DockStyle.Bottom;
        _buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        _buttonPanel.Height        = 38;
        _buttonPanel.Padding       = new System.Windows.Forms.Padding(4);

        // ── _gridPanel ────────────────────────────────────────────────────────────
        // Row 0: Project Name  (28px)
        // Row 1: Location      (28px)
        // Row 2: Full path     (26px)
        // Row 3: separator     (6px)
        // Row 4: Game .csproj  (28px)
        // Row 5: Content       (28px)
        // Row 6: Localization  (28px)
        _gridPanel.ColumnCount = 2;
        _gridPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
        _gridPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _gridPanel.Controls.Add(_nameLabel,          0, 0);
        _gridPanel.Controls.Add(_nameTextBox,         1, 0);
        _gridPanel.Controls.Add(_locationLabel,       0, 1);
        _gridPanel.Controls.Add(_locationRow,         1, 1);
        _gridPanel.Controls.Add(_previewLabel,        0, 2);
        _gridPanel.Controls.Add(_previewValueLabel,   1, 2);
        _gridPanel.Controls.Add(_separatorLabel,      0, 3);
        _gridPanel.SetColumnSpan(_separatorLabel, 2);
        _gridPanel.Controls.Add(_csprojLabel,         0, 4);
        _gridPanel.Controls.Add(_csprojRow,           1, 4);
        _gridPanel.Controls.Add(_contentLabel,        0, 5);
        _gridPanel.Controls.Add(_contentRow,          1, 5);
        _gridPanel.Controls.Add(_localizationLabel,   0, 6);
        _gridPanel.Controls.Add(_localizationRow,     1, 6);
        _gridPanel.Dock     = System.Windows.Forms.DockStyle.Top;
        _gridPanel.Height   = 204;
        _gridPanel.Padding  = new System.Windows.Forms.Padding(8);
        _gridPanel.RowCount = 7;
        _gridPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
        _gridPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
        _gridPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
        _gridPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 8F));
        _gridPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
        _gridPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
        _gridPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));

        // ── NewProjectDialog ──────────────────────────────────────────────────────
        AcceptButton    = _okButton;
        CancelButton    = _cancelButton;
        ClientSize      = new System.Drawing.Size(500, 256);
        Controls.Add(_gridPanel);
        Controls.Add(_buttonPanel);
        Font            = new System.Drawing.Font("Segoe UI", 9F);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = System.Windows.Forms.FormStartPosition.CenterParent;
        Text            = "New Project";

        _gridPanel.ResumeLayout(false);
        _locationRow.ResumeLayout(false);
        _csprojRow.ResumeLayout(false);
        _contentRow.ResumeLayout(false);
        _localizationRow.ResumeLayout(false);
        _buttonPanel.ResumeLayout(false);
        ResumeLayout(false);
    }
}
