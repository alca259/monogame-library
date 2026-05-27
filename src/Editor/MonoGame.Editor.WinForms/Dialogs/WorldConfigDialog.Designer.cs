#nullable enable
namespace MonoGame.Editor.WinForms.Dialogs;

partial class WorldConfigDialog
{
    private System.ComponentModel.IContainer? components = null;

    // Physics
    private System.Windows.Forms.GroupBox         _physicsGroup   = null!;
    private System.Windows.Forms.CheckBox         _physicsCheck   = null!;
    private System.Windows.Forms.Label            _gravityXLabel  = null!;
    private System.Windows.Forms.NumericUpDown    _gravityXBox    = null!;
    private System.Windows.Forms.Label            _gravityYLabel  = null!;
    private System.Windows.Forms.NumericUpDown    _gravityYBox    = null!;

    // Lighting
    private System.Windows.Forms.GroupBox         _lightingGroup       = null!;
    private System.Windows.Forms.CheckBox         _lightingCheck       = null!;
    private System.Windows.Forms.Label            _ambientLabel        = null!;
    private System.Windows.Forms.Panel            _ambientColorPanel   = null!;
    private System.Windows.Forms.Button           _ambientColorButton  = null!;

    // Navigation
    private System.Windows.Forms.GroupBox         _navGroup       = null!;
    private System.Windows.Forms.CheckBox         _navCheck       = null!;
    private System.Windows.Forms.Label            _navWidthLabel  = null!;
    private System.Windows.Forms.NumericUpDown    _navWidthBox    = null!;
    private System.Windows.Forms.Label            _navHeightLabel = null!;
    private System.Windows.Forms.NumericUpDown    _navHeightBox   = null!;
    private System.Windows.Forms.Label            _navCellLabel   = null!;
    private System.Windows.Forms.NumericUpDown    _navCellSizeBox = null!;
    private System.Windows.Forms.Label            _navOriginXLabel = null!;
    private System.Windows.Forms.NumericUpDown    _navOriginXBox  = null!;
    private System.Windows.Forms.Label            _navOriginYLabel = null!;
    private System.Windows.Forms.NumericUpDown    _navOriginYBox  = null!;

    // Audio
    private System.Windows.Forms.GroupBox         _audioGroup     = null!;
    private System.Windows.Forms.CheckBox         _audioCheck     = null!;

    // Buttons
    private System.Windows.Forms.FlowLayoutPanel  _buttonPanel    = null!;
    private System.Windows.Forms.Button           _cancelButton   = null!;
    private System.Windows.Forms.Button           _okButton       = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        _physicsGroup       = new System.Windows.Forms.GroupBox();
        _physicsCheck       = new System.Windows.Forms.CheckBox();
        _gravityXLabel      = new System.Windows.Forms.Label();
        _gravityXBox        = new System.Windows.Forms.NumericUpDown();
        _gravityYLabel      = new System.Windows.Forms.Label();
        _gravityYBox        = new System.Windows.Forms.NumericUpDown();

        _lightingGroup      = new System.Windows.Forms.GroupBox();
        _lightingCheck      = new System.Windows.Forms.CheckBox();
        _ambientLabel       = new System.Windows.Forms.Label();
        _ambientColorPanel  = new System.Windows.Forms.Panel();
        _ambientColorButton = new System.Windows.Forms.Button();

        _navGroup           = new System.Windows.Forms.GroupBox();
        _navCheck           = new System.Windows.Forms.CheckBox();
        _navWidthLabel      = new System.Windows.Forms.Label();
        _navWidthBox        = new System.Windows.Forms.NumericUpDown();
        _navHeightLabel     = new System.Windows.Forms.Label();
        _navHeightBox       = new System.Windows.Forms.NumericUpDown();
        _navCellLabel       = new System.Windows.Forms.Label();
        _navCellSizeBox     = new System.Windows.Forms.NumericUpDown();
        _navOriginXLabel    = new System.Windows.Forms.Label();
        _navOriginXBox      = new System.Windows.Forms.NumericUpDown();
        _navOriginYLabel    = new System.Windows.Forms.Label();
        _navOriginYBox      = new System.Windows.Forms.NumericUpDown();

        _audioGroup         = new System.Windows.Forms.GroupBox();
        _audioCheck         = new System.Windows.Forms.CheckBox();

        _buttonPanel        = new System.Windows.Forms.FlowLayoutPanel();
        _cancelButton       = new System.Windows.Forms.Button();
        _okButton           = new System.Windows.Forms.Button();

        _physicsGroup.SuspendLayout();
        _lightingGroup.SuspendLayout();
        _navGroup.SuspendLayout();
        _audioGroup.SuspendLayout();
        _buttonPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_gravityXBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_gravityYBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_navWidthBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_navHeightBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_navCellSizeBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_navOriginXBox).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_navOriginYBox).BeginInit();
        SuspendLayout();

        // ── Physics group ─────────────────────────────────────────────────────
        _physicsCheck.AutoSize  = true;
        _physicsCheck.Location  = new System.Drawing.Point(8, 20);
        _physicsCheck.Name      = "_physicsCheck";
        _physicsCheck.Text      = "Enable Physics 2D";
        _physicsCheck.TabIndex  = 0;
        _physicsCheck.CheckedChanged += OnPhysicsCheckChanged;

        _gravityXLabel.AutoSize    = true;
        _gravityXLabel.Location    = new System.Drawing.Point(8, 46);
        _gravityXLabel.Text        = "Gravity X:";

        _gravityXBox.DecimalPlaces = 2;
        _gravityXBox.Minimum       = new decimal(new int[] { 100000, 0, 0, unchecked((int)0x80000000) });
        _gravityXBox.Maximum       = new decimal(new int[] { 100000, 0, 0, 0 });
        _gravityXBox.Location      = new System.Drawing.Point(90, 44);
        _gravityXBox.Name          = "_gravityXBox";
        _gravityXBox.Size          = new System.Drawing.Size(90, 23);
        _gravityXBox.TabIndex      = 1;

        _gravityYLabel.AutoSize    = true;
        _gravityYLabel.Location    = new System.Drawing.Point(195, 46);
        _gravityYLabel.Text        = "Gravity Y:";

        _gravityYBox.DecimalPlaces = 2;
        _gravityYBox.Minimum       = new decimal(new int[] { 100000, 0, 0, unchecked((int)0x80000000) });
        _gravityYBox.Maximum       = new decimal(new int[] { 100000, 0, 0, 0 });
        _gravityYBox.Value         = new decimal(new int[] { 98, 0, 0, unchecked((int)0x80010000) }); // -9.8
        _gravityYBox.Location      = new System.Drawing.Point(280, 44);
        _gravityYBox.Name          = "_gravityYBox";
        _gravityYBox.Size          = new System.Drawing.Size(90, 23);
        _gravityYBox.TabIndex      = 2;

        _physicsGroup.Controls.Add(_physicsCheck);
        _physicsGroup.Controls.Add(_gravityXLabel);
        _physicsGroup.Controls.Add(_gravityXBox);
        _physicsGroup.Controls.Add(_gravityYLabel);
        _physicsGroup.Controls.Add(_gravityYBox);
        _physicsGroup.Dock     = System.Windows.Forms.DockStyle.Top;
        _physicsGroup.Height   = 78;
        _physicsGroup.Name     = "_physicsGroup";
        _physicsGroup.TabIndex = 0;
        _physicsGroup.Text     = "Physics 2D";

        // ── Lighting group ────────────────────────────────────────────────────
        _lightingCheck.AutoSize  = true;
        _lightingCheck.Location  = new System.Drawing.Point(8, 20);
        _lightingCheck.Name      = "_lightingCheck";
        _lightingCheck.Text      = "Enable Lighting";
        _lightingCheck.TabIndex  = 0;
        _lightingCheck.CheckedChanged += OnLightingCheckChanged;

        _ambientLabel.AutoSize  = true;
        _ambientLabel.Location  = new System.Drawing.Point(8, 46);
        _ambientLabel.Text      = "Ambient color:";

        _ambientColorPanel.BackColor = System.Drawing.Color.Black;
        _ambientColorPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        _ambientColorPanel.Location = new System.Drawing.Point(100, 44);
        _ambientColorPanel.Name     = "_ambientColorPanel";
        _ambientColorPanel.Size     = new System.Drawing.Size(24, 24);
        _ambientColorPanel.TabStop  = false;

        _ambientColorButton.Location = new System.Drawing.Point(130, 44);
        _ambientColorButton.Name     = "_ambientColorButton";
        _ambientColorButton.Size     = new System.Drawing.Size(30, 24);
        _ambientColorButton.TabIndex = 1;
        _ambientColorButton.Text     = "...";
        _ambientColorButton.Click   += OnAmbientColorButtonClick;

        _lightingGroup.Controls.Add(_lightingCheck);
        _lightingGroup.Controls.Add(_ambientLabel);
        _lightingGroup.Controls.Add(_ambientColorPanel);
        _lightingGroup.Controls.Add(_ambientColorButton);
        _lightingGroup.Dock     = System.Windows.Forms.DockStyle.Top;
        _lightingGroup.Height   = 78;
        _lightingGroup.Name     = "_lightingGroup";
        _lightingGroup.TabIndex = 1;
        _lightingGroup.Text     = "Lighting";

        // ── Navigation group ──────────────────────────────────────────────────
        _navCheck.AutoSize  = true;
        _navCheck.Location  = new System.Drawing.Point(8, 20);
        _navCheck.Name      = "_navCheck";
        _navCheck.Text      = "Enable Navigation";
        _navCheck.TabIndex  = 0;
        _navCheck.CheckedChanged += OnNavCheckChanged;

        int navRow1Y = 46, navRow2Y = 74;

        _navWidthLabel.AutoSize  = true;
        _navWidthLabel.Location  = new System.Drawing.Point(8, navRow1Y + 2);
        _navWidthLabel.Text      = "Width (cells):";

        _navWidthBox.Minimum     = new decimal(new int[] { 1, 0, 0, 0 });
        _navWidthBox.Maximum     = new decimal(new int[] { 4096, 0, 0, 0 });
        _navWidthBox.Value       = new decimal(new int[] { 32, 0, 0, 0 });
        _navWidthBox.Location    = new System.Drawing.Point(90, navRow1Y);
        _navWidthBox.Name        = "_navWidthBox";
        _navWidthBox.Size        = new System.Drawing.Size(70, 23);
        _navWidthBox.TabIndex    = 1;

        _navHeightLabel.AutoSize = true;
        _navHeightLabel.Location = new System.Drawing.Point(175, navRow1Y + 2);
        _navHeightLabel.Text     = "Height (cells):";

        _navHeightBox.Minimum    = new decimal(new int[] { 1, 0, 0, 0 });
        _navHeightBox.Maximum    = new decimal(new int[] { 4096, 0, 0, 0 });
        _navHeightBox.Value      = new decimal(new int[] { 32, 0, 0, 0 });
        _navHeightBox.Location   = new System.Drawing.Point(270, navRow1Y);
        _navHeightBox.Name       = "_navHeightBox";
        _navHeightBox.Size       = new System.Drawing.Size(70, 23);
        _navHeightBox.TabIndex   = 2;

        _navCellLabel.AutoSize   = true;
        _navCellLabel.Location   = new System.Drawing.Point(8, navRow2Y + 2);
        _navCellLabel.Text       = "Cell size (px):";

        _navCellSizeBox.DecimalPlaces = 1;
        _navCellSizeBox.Minimum  = new decimal(new int[] { 1, 0, 0, 0 });
        _navCellSizeBox.Maximum  = new decimal(new int[] { 1024, 0, 0, 0 });
        _navCellSizeBox.Value    = new decimal(new int[] { 32, 0, 0, 0 });
        _navCellSizeBox.Location = new System.Drawing.Point(90, navRow2Y);
        _navCellSizeBox.Name     = "_navCellSizeBox";
        _navCellSizeBox.Size     = new System.Drawing.Size(70, 23);
        _navCellSizeBox.TabIndex = 3;

        _navOriginXLabel.AutoSize  = true;
        _navOriginXLabel.Location  = new System.Drawing.Point(175, navRow2Y + 2);
        _navOriginXLabel.Text      = "Origin X:";

        _navOriginXBox.DecimalPlaces = 1;
        _navOriginXBox.Minimum     = new decimal(new int[] { 100000, 0, 0, unchecked((int)0x80000000) });
        _navOriginXBox.Maximum     = new decimal(new int[] { 100000, 0, 0, 0 });
        _navOriginXBox.Location    = new System.Drawing.Point(240, navRow2Y);
        _navOriginXBox.Name        = "_navOriginXBox";
        _navOriginXBox.Size        = new System.Drawing.Size(70, 23);
        _navOriginXBox.TabIndex    = 4;

        _navOriginYLabel.AutoSize  = true;
        _navOriginYLabel.Location  = new System.Drawing.Point(320, navRow2Y + 2);
        _navOriginYLabel.Text      = "Origin Y:";

        _navOriginYBox.DecimalPlaces = 1;
        _navOriginYBox.Minimum     = new decimal(new int[] { 100000, 0, 0, unchecked((int)0x80000000) });
        _navOriginYBox.Maximum     = new decimal(new int[] { 100000, 0, 0, 0 });
        _navOriginYBox.Location    = new System.Drawing.Point(370, navRow2Y);
        _navOriginYBox.Name        = "_navOriginYBox";
        _navOriginYBox.Size        = new System.Drawing.Size(70, 23);
        _navOriginYBox.TabIndex    = 5;

        _navGroup.Controls.Add(_navCheck);
        _navGroup.Controls.Add(_navWidthLabel);
        _navGroup.Controls.Add(_navWidthBox);
        _navGroup.Controls.Add(_navHeightLabel);
        _navGroup.Controls.Add(_navHeightBox);
        _navGroup.Controls.Add(_navCellLabel);
        _navGroup.Controls.Add(_navCellSizeBox);
        _navGroup.Controls.Add(_navOriginXLabel);
        _navGroup.Controls.Add(_navOriginXBox);
        _navGroup.Controls.Add(_navOriginYLabel);
        _navGroup.Controls.Add(_navOriginYBox);
        _navGroup.Dock     = System.Windows.Forms.DockStyle.Top;
        _navGroup.Height   = 108;
        _navGroup.Name     = "_navGroup";
        _navGroup.TabIndex = 2;
        _navGroup.Text     = "Navigation";

        // ── Audio group ───────────────────────────────────────────────────────
        _audioCheck.AutoSize  = true;
        _audioCheck.Location  = new System.Drawing.Point(8, 20);
        _audioCheck.Name      = "_audioCheck";
        _audioCheck.Text      = "Enable AudioController";
        _audioCheck.TabIndex  = 0;

        _audioGroup.Controls.Add(_audioCheck);
        _audioGroup.Dock     = System.Windows.Forms.DockStyle.Top;
        _audioGroup.Height   = 50;
        _audioGroup.Name     = "_audioGroup";
        _audioGroup.TabIndex = 3;
        _audioGroup.Text     = "Audio";

        // ── Buttons ───────────────────────────────────────────────────────────
        _cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        _cancelButton.Name     = "_cancelButton";
        _cancelButton.Size     = new System.Drawing.Size(75, 23);
        _cancelButton.TabIndex = 1;
        _cancelButton.Text     = "Cancel";

        _okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
        _okButton.Name         = "_okButton";
        _okButton.Size         = new System.Drawing.Size(75, 23);
        _okButton.TabIndex     = 0;
        _okButton.Text         = "OK";

        _buttonPanel.Controls.Add(_okButton);
        _buttonPanel.Controls.Add(_cancelButton);
        _buttonPanel.Dock          = System.Windows.Forms.DockStyle.Bottom;
        _buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        _buttonPanel.Height        = 36;
        _buttonPanel.Name          = "_buttonPanel";
        _buttonPanel.Padding       = new System.Windows.Forms.Padding(4);
        _buttonPanel.TabIndex      = 4;

        // ── Form ──────────────────────────────────────────────────────────────
        AcceptButton          = _okButton;
        AutoScaleDimensions   = new System.Drawing.SizeF(7f, 15f);
        AutoScaleMode         = System.Windows.Forms.AutoScaleMode.Font;
        CancelButton          = _cancelButton;
        ClientSize            = new System.Drawing.Size(460, 350);
        Controls.Add(_buttonPanel);
        Controls.Add(_navGroup);
        Controls.Add(_lightingGroup);
        Controls.Add(_physicsGroup);
        Controls.Add(_audioGroup);
        Font                  = new System.Drawing.Font("Segoe UI", 9f);
        FormBorderStyle       = System.Windows.Forms.FormBorderStyle.FixedDialog;
        MaximizeBox           = false;
        MinimizeBox           = false;
        Name                  = "WorldConfigDialog";
        StartPosition         = System.Windows.Forms.FormStartPosition.CenterParent;
        Text                  = "Configure World Subsystems";

        _physicsGroup.ResumeLayout(false);
        _physicsGroup.PerformLayout();
        _lightingGroup.ResumeLayout(false);
        _lightingGroup.PerformLayout();
        _navGroup.ResumeLayout(false);
        _navGroup.PerformLayout();
        _audioGroup.ResumeLayout(false);
        _audioGroup.PerformLayout();
        _buttonPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_gravityXBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)_gravityYBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)_navWidthBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)_navHeightBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)_navCellSizeBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)_navOriginXBox).EndInit();
        ((System.ComponentModel.ISupportInitialize)_navOriginYBox).EndInit();
        ResumeLayout(false);
        UpdateGroupBoxStates();
    }
}
