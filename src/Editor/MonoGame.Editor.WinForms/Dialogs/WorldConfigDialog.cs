using MonoGame.Editor.Core.Models;

namespace MonoGame.Editor.WinForms.Dialogs;

/// <summary>Dialog for configuring optional GameWorld subsystems for a scene.</summary>
public sealed partial class WorldConfigDialog : Form
{
    private System.Drawing.Color _ambientColor = System.Drawing.Color.Black;

    /// <summary>Initializes the dialog.</summary>
    public WorldConfigDialog() => InitializeComponent();

    /// <summary>Loads existing config values into the dialog controls.</summary>
    public void LoadFrom(EditorWorldConfig? cfg)
    {
        if (cfg is null) return;

        _physicsCheck.Checked     = cfg.UsePhysics2D;
        _gravityXBox.Value        = (decimal)cfg.GravityX;
        _gravityYBox.Value        = (decimal)cfg.GravityY;

        _lightingCheck.Checked    = cfg.UseLighting;
        int[] c = cfg.AmbientColorRgba;
        _ambientColor             = System.Drawing.Color.FromArgb(c[3], c[0], c[1], c[2]);
        _ambientColorPanel.BackColor = _ambientColor;

        _navCheck.Checked         = cfg.UseNavigation;
        _navWidthBox.Value        = cfg.NavGridWidth;
        _navHeightBox.Value       = cfg.NavGridHeight;
        _navCellSizeBox.Value     = (decimal)cfg.NavGridCellSize;
        _navOriginXBox.Value      = (decimal)cfg.NavGridOriginX;
        _navOriginYBox.Value      = (decimal)cfg.NavGridOriginY;

        _audioCheck.Checked       = cfg.UseAudio;

        UpdateGroupBoxStates();
    }

    /// <summary>Builds an <see cref="EditorWorldConfig"/> from the current dialog values.</summary>
    public EditorWorldConfig BuildConfig()
    {
        return new EditorWorldConfig
        {
            UsePhysics2D      = _physicsCheck.Checked,
            GravityX          = (float)_gravityXBox.Value,
            GravityY          = (float)_gravityYBox.Value,

            UseLighting       = _lightingCheck.Checked,
            AmbientColorRgba  = [_ambientColor.R, _ambientColor.G, _ambientColor.B, _ambientColor.A],

            UseNavigation     = _navCheck.Checked,
            NavGridWidth      = (int)_navWidthBox.Value,
            NavGridHeight     = (int)_navHeightBox.Value,
            NavGridCellSize   = (float)_navCellSizeBox.Value,
            NavGridOriginX    = (float)_navOriginXBox.Value,
            NavGridOriginY    = (float)_navOriginYBox.Value,

            UseAudio          = _audioCheck.Checked,
        };
    }

    private void OnPhysicsCheckChanged(object? sender, EventArgs e)   => UpdateGroupBoxStates();
    private void OnLightingCheckChanged(object? sender, EventArgs e)  => UpdateGroupBoxStates();
    private void OnNavCheckChanged(object? sender, EventArgs e)       => UpdateGroupBoxStates();

    private void UpdateGroupBoxStates()
    {
        foreach (Control ctrl in _physicsGroup.Controls)
            if (ctrl != _physicsCheck) ctrl.Enabled = _physicsCheck.Checked;

        foreach (Control ctrl in _lightingGroup.Controls)
            if (ctrl != _lightingCheck) ctrl.Enabled = _lightingCheck.Checked;

        foreach (Control ctrl in _navGroup.Controls)
            if (ctrl != _navCheck) ctrl.Enabled = _navCheck.Checked;
    }

    private void OnAmbientColorButtonClick(object? sender, EventArgs e)
    {
        using ColorDialog dlg = new() { Color = _ambientColor, FullOpen = true };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _ambientColor = dlg.Color;
        _ambientColorPanel.BackColor = _ambientColor;
    }
}
