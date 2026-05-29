namespace MonoGame.Editor.WinForms.Dialogs;

/// <summary>Diálogo para crear una nueva escena.</summary>
public sealed partial class NewSceneDialog : Form
{
    /// <summary>Inicializa el diálogo.</summary>
    public NewSceneDialog() => InitializeComponent();

    /// <summary>Nombre de la escena introducido por el usuario.</summary>
    public string SceneName => _nameBox.Text.Trim();

    /// <summary>Anchura opcional del mundo en píxeles (0 = sin límite).</summary>
    public float WorldWidth => (float)_widthBox.Value;

    /// <summary>Altura opcional del mundo en píxeles (0 = sin límite).</summary>
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
