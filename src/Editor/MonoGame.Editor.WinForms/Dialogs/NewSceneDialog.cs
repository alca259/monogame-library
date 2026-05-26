namespace MonoGame.Editor.WinForms.Dialogs;

/// <summary>Dialog for creating a new scene.</summary>
public sealed partial class NewSceneDialog : Form
{
    /// <summary>Initializes the dialog.</summary>
    public NewSceneDialog() => InitializeComponent();

    /// <summary>Scene name entered by the user.</summary>
    public string SceneName => _nameBox.Text.Trim();

    /// <summary>Optional world width in pixels (0 = unbounded).</summary>
    public float WorldWidth => (float)_widthBox.Value;

    /// <summary>Optional world height in pixels (0 = unbounded).</summary>
    public float WorldHeight => (float)_heightBox.Value;

    private void OnNameBoxTextChanged(object? sender, EventArgs e) => UpdatePreview();

    private void UpdatePreview()
    {
        string name = SceneName;
        bool valid = !string.IsNullOrWhiteSpace(name);
        _okButton.Enabled = valid;
        _previewValueLabel.Text = valid ? $"{name}.scene.json" : string.Empty;
    }
}
