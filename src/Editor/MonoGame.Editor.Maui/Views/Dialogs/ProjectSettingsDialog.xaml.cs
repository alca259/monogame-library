namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed partial class ProjectSettingsDialog : ContentPage
{
    private static readonly string[] BuildConfigs = ["Debug", "Release"];

    private readonly TaskCompletionSource<bool> _tcs = new();
    private readonly EditorProject   _project;
    private readonly ProjectSettings _settings;

    private ScrollView _activePanel;

    private ProjectSettingsDialog(EditorProject project, ProjectSettings settings)
    {
        InitializeComponent();
        _project  = project;
        _settings = settings;
        _activePanel = GeneralPanel;

        foreach (string cfg in BuildConfigs)
            BuildConfigPicker.Items.Add(cfg);

        LoadFromSettings();
    }

    public static async Task<bool> ShowAsync(INavigation navigation,
                                              EditorProject project,
                                              ProjectSettings settings)
    {
        var dialog = new ProjectSettingsDialog(project, settings);
        await navigation.PushModalAsync(dialog);
        return await dialog._tcs.Task;
    }

    // ── Load / Save ───────────────────────────────────────────────────────────

    private void LoadFromSettings()
    {
        // General
        int cfgIdx = Array.IndexOf(BuildConfigs, _settings.BuildConfiguration);
        BuildConfigPicker.SelectedIndex = cfgIdx >= 0 ? cfgIdx : 0;
        VirtualWidthEntry.Text          = _settings.VirtualWidth.ToString();
        VirtualHeightEntry.Text         = _settings.VirtualHeight.ToString();
        GameAppCsprojEntry.Text         = _settings.GameAppCsprojRelPath;
        GameScriptsCsprojEntry.Text     = _settings.GameScriptsCsprojRelPath;

        // Content
        ContentRelPathEntry.Text        = _settings.ContentRelPath;
        LocalizationRelPathEntry.Text   = _settings.LocalizationRelPath;

        // Localization
        DefaultLocaleEntry.Text         = _settings.DefaultLocale;
        SupportedLocalesEntry.Text      = string.Join(", ", _settings.SupportedLocales);

        // CodeGen
        RootNamespaceEntry.Text         = _settings.RootNamespace;
        GeneratedCodeFolderEntry.Text   = _settings.GeneratedCodeFolder;
        GenerateOnSaveCheck.IsChecked   = _settings.GenerateOnSave;
    }

    private void SaveToSettings()
    {
        // General
        _settings.BuildConfiguration   = BuildConfigPicker.SelectedItem as string ?? "Debug";
        _settings.VirtualWidth         = ParseInt(VirtualWidthEntry.Text, 1920);
        _settings.VirtualHeight        = ParseInt(VirtualHeightEntry.Text, 1080);
        _settings.GameAppCsprojRelPath  = GameAppCsprojEntry.Text?.Trim()     ?? string.Empty;
        _settings.GameScriptsCsprojRelPath = GameScriptsCsprojEntry.Text?.Trim() ?? string.Empty;

        // Content
        _settings.ContentRelPath       = ContentRelPathEntry.Text?.Trim()     ?? "Content";
        _settings.LocalizationRelPath  = LocalizationRelPathEntry.Text?.Trim() ?? "Localization";

        // Localization
        _settings.DefaultLocale        = DefaultLocaleEntry.Text?.Trim()      ?? "en-US";
        _settings.SupportedLocales     = ParseLocaleList(SupportedLocalesEntry.Text);

        // CodeGen
        _settings.RootNamespace        = RootNamespaceEntry.Text?.Trim()      ?? string.Empty;
        _settings.GeneratedCodeFolder  = GeneratedCodeFolderEntry.Text?.Trim() ?? "Generated";
        _settings.GenerateOnSave       = GenerateOnSaveCheck.IsChecked;
    }

    // ── Tab switching ─────────────────────────────────────────────────────────

    private void OnTabClicked(object sender, EventArgs e)
    {
        string tag = (sender as Button)?.CommandParameter as string ?? "General";
        _activePanel.IsVisible = false;
        _activePanel = tag switch
        {
            "Content"      => ContentPanel,
            "Localization" => LocalizationPanel,
            "CodeGen"      => CodeGenPanel,
            _              => GeneralPanel,
        };
        _activePanel.IsVisible = true;
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    protected override bool OnBackButtonPressed()
    {
        _tcs.TrySetResult(false);
        return base.OnBackButtonPressed();
    }

    private void OnCancel(object sender, EventArgs e)
    {
        _tcs.TrySetResult(false);
        _ = Navigation.PopModalAsync();
    }

    private async void OnSubmit(object sender, EventArgs e)
    {
        SaveToSettings();
        await _settings.SaveAsync(_project).ConfigureAwait(false);
        _tcs.TrySetResult(true);
        await Navigation.PopModalAsync().ConfigureAwait(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int ParseInt(string? text, int fallback)
        => int.TryParse(text, out int v) && v > 0 ? v : fallback;

    private static List<string> ParseLocaleList(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return ["en-US"];
        List<string> result = [];
        foreach (string part in text.Split(','))
        {
            string trimmed = part.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                result.Add(trimmed);
        }
        return result.Count > 0 ? result : ["en-US"];
    }
}
