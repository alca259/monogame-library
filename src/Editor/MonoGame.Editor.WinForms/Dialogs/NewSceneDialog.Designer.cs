#nullable enable
namespace MonoGame.Editor.WinForms.Dialogs;

partial class NewSceneDialog
{
    private System.ComponentModel.IContainer? components = null;

    private System.Windows.Forms.TableLayoutPanel _gridPanel = null!;
    private System.Windows.Forms.Label _nameLabel = null!;
    private System.Windows.Forms.TextBox _nameBox = null!;
    private System.Windows.Forms.Label _widthLabel = null!;
    private System.Windows.Forms.NumericUpDown _widthBox = null!;
    private System.Windows.Forms.Label _heightLabel = null!;
    private System.Windows.Forms.NumericUpDown _heightBox = null!;
    private System.Windows.Forms.Label _previewLabel = null!;
    private System.Windows.Forms.Label _previewValueLabel = null!;
    private System.Windows.Forms.FlowLayoutPanel _buttonPanel = null!;
    private System.Windows.Forms.Button _cancelButton = null!;
    private System.Windows.Forms.Button _okButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        _gridPanel        = new System.Windows.Forms.TableLayoutPanel();
        _nameLabel        = new System.Windows.Forms.Label();
        _nameBox          = new System.Windows.Forms.TextBox();
        _widthLabel       = new System.Windows.Forms.Label();
        _widthBox         = new System.Windows.Forms.NumericUpDown();
        _heightLabel      = new System.Windows.Forms.Label();
        _heightBox        = new System.Windows.Forms.NumericUpDown();
        _previewLabel     = new System.Windows.Forms.Label();
        _previewValueLabel = new System.Windows.Forms.Label();
        _buttonPanel      = new System.Windows.Forms.FlowLayoutPanel();
        _cancelButton     = new System.Windows.Forms.Button();
        _okButton         = new System.Windows.Forms.Button();

        _gridPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_widthBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_heightBox).BeginInit();
        _buttonPanel.SuspendLayout();
        SuspendLayout();

        // _gridPanel
        _gridPanel.ColumnCount = 2;
        _gridPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
        _gridPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        _gridPanel.Controls.Add(_nameLabel, 0, 0);
        _gridPanel.Controls.Add(_nameBox, 1, 0);
        _gridPanel.Controls.Add(_widthLabel, 0, 1);
        _gridPanel.Controls.Add(_widthBox, 1, 1);
        _gridPanel.Controls.Add(_heightLabel, 0, 2);
        _gridPanel.Controls.Add(_heightBox, 1, 2);
        _gridPanel.Controls.Add(_previewLabel, 0, 3);
        _gridPanel.Controls.Add(_previewValueLabel, 1, 3);
        _gridPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        _gridPanel.Location = new System.Drawing.Point(0, 0);
        _gridPanel.Name = "_gridPanel";
        _gridPanel.Padding = new System.Windows.Forms.Padding(8, 8, 8, 0);
        _gridPanel.RowCount = 4;
        _gridPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
        _gridPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
        _gridPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
        _gridPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
        _gridPanel.Size = new System.Drawing.Size(380, 112);
        _gridPanel.TabIndex = 0;

        // _nameLabel
        _nameLabel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _nameLabel.AutoSize = true;
        _nameLabel.Name = "_nameLabel";
        _nameLabel.Size = new System.Drawing.Size(80, 15);
        _nameLabel.Text = "Scene Name:";
        _nameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        // _nameBox
        _nameBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _nameBox.Name = "_nameBox";
        _nameBox.Size = new System.Drawing.Size(250, 23);
        _nameBox.TabIndex = 0;
        _nameBox.TextChanged += OnNameBoxTextChanged;

        // _widthLabel
        _widthLabel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _widthLabel.AutoSize = true;
        _widthLabel.Name = "_widthLabel";
        _widthLabel.Size = new System.Drawing.Size(80, 15);
        _widthLabel.Text = "World Width:";
        _widthLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        // _widthBox
        _widthBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _widthBox.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        _widthBox.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
        _widthBox.Name = "_widthBox";
        _widthBox.Size = new System.Drawing.Size(250, 23);
        _widthBox.TabIndex = 1;

        // _heightLabel
        _heightLabel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _heightLabel.AutoSize = true;
        _heightLabel.Name = "_heightLabel";
        _heightLabel.Size = new System.Drawing.Size(80, 15);
        _heightLabel.Text = "World Height:";
        _heightLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        // _heightBox
        _heightBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _heightBox.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
        _heightBox.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
        _heightBox.Name = "_heightBox";
        _heightBox.Size = new System.Drawing.Size(250, 23);
        _heightBox.TabIndex = 2;

        // _previewLabel
        _previewLabel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _previewLabel.AutoSize = true;
        _previewLabel.Name = "_previewLabel";
        _previewLabel.Size = new System.Drawing.Size(80, 15);
        _previewLabel.Text = "Preview:";

        // _previewValueLabel
        _previewValueLabel.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        _previewValueLabel.AutoSize = true;
        _previewValueLabel.ForeColor = System.Drawing.SystemColors.GrayText;
        _previewValueLabel.Name = "_previewValueLabel";
        _previewValueLabel.Size = new System.Drawing.Size(250, 15);
        _previewValueLabel.Text = string.Empty;

        // _cancelButton
        _cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        _cancelButton.Name = "_cancelButton";
        _cancelButton.Size = new System.Drawing.Size(75, 23);
        _cancelButton.TabIndex = 1;
        _cancelButton.Text = "Cancel";

        // _okButton
        _okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
        _okButton.Enabled = false;
        _okButton.Name = "_okButton";
        _okButton.Size = new System.Drawing.Size(75, 23);
        _okButton.TabIndex = 0;
        _okButton.Text = "OK";

        // _buttonPanel
        _buttonPanel.Controls.Add(_okButton);
        _buttonPanel.Controls.Add(_cancelButton);
        _buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
        _buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        _buttonPanel.Location = new System.Drawing.Point(0, 112);
        _buttonPanel.Name = "_buttonPanel";
        _buttonPanel.Padding = new System.Windows.Forms.Padding(4);
        _buttonPanel.Size = new System.Drawing.Size(380, 36);
        _buttonPanel.TabIndex = 1;

        // NewSceneDialog
        AcceptButton = _okButton;
        AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        CancelButton = _cancelButton;
        ClientSize = new System.Drawing.Size(380, 148);
        Controls.Add(_gridPanel);
        Controls.Add(_buttonPanel);
        Font = new System.Drawing.Font("Segoe UI", 9f);
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "NewSceneDialog";
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        Text = "New Scene";

        _gridPanel.ResumeLayout(false);
        _gridPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_widthBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)_heightBox).EndInit();
        _buttonPanel.ResumeLayout(false);
        ResumeLayout(false);
    }
}
