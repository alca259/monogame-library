namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed partial class ProjectSettingsDialog : ContentPage
{
    private static readonly string[] BuildConfigs = ["Debug", "Release"];

    private static readonly Color ActiveTabFg   = Color.FromArgb("#E6E6E8");
    private static readonly Color InactiveTabFg = Color.FromArgb("#9A9AA2");
    private static readonly Color ActiveTabBg   = Color.FromArgb("#2D2D33");

    private readonly TaskCompletionSource<bool> _tcs = new();
    private readonly EditorProject   _project;
    private readonly ProjectSettings _settings;

    private ScrollView _activePanel;
    private Button?    _activeTabBtn;

    private ProjectSettingsDialog(EditorProject project, ProjectSettings settings)
    {
        InitializeComponent();
        _project  = project;
        _settings = settings;
        _activePanel = GeneralPanel;

        foreach (string cfg in BuildConfigs)
            BuildConfigPicker.Items.Add(cfg);

        LoadFromSettings();
        SetActiveTab(GeneralTabBtn);
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

        GameAppCsprojEntry.Text = !string.IsNullOrEmpty(_settings.GameAppCsprojRelPath)
            ? _settings.GameAppCsprojRelPath
            : (!string.IsNullOrEmpty(_project.GameCsprojPath)
                ? Path.GetRelativePath(_project.RootPath, _project.GameCsprojPath)
                : string.Empty);

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

    private void SetActiveTab(Button btn)
    {
        if (_activeTabBtn is not null)
        {
            _activeTabBtn.TextColor       = InactiveTabFg;
            _activeTabBtn.BackgroundColor = Colors.Transparent;
        }
        _activeTabBtn         = btn;
        btn.TextColor         = ActiveTabFg;
        btn.BackgroundColor   = ActiveTabBg;
    }

    private void OnTabClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        string tag = btn.CommandParameter as string ?? "General";
        _activePanel.IsVisible = false;
        _activePanel = tag switch
        {
            "Content"      => ContentPanel,
            "Localization" => LocalizationPanel,
            "CodeGen"      => CodeGenPanel,
            _              => GeneralPanel,
        };
        _activePanel.IsVisible = true;
        SetActiveTab(btn);
    }

    private async void OnBrowseLocalizationFolderClicked(object sender, EventArgs e)
    {
        string? rel = await PickFolderRelativeToProjectAsync().ConfigureAwait(true);
        if (rel is not null) LocalizationRelPathEntry.Text = rel;
    }

    private async Task<string?> PickFolderRelativeToProjectAsync()
    {
        try
        {
            Microsoft.UI.Xaml.Window? win = Application.Current?.Windows.FirstOrDefault()
                ?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (win is null) return null;

            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(win);
            var picker = new Windows.Storage.Pickers.FolderPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            Windows.Storage.StorageFolder? folder = await picker.PickSingleFolderAsync();
            if (folder is null) return null;

            return Path.GetRelativePath(_project.RootPath, folder.Path);
        }
        catch
        {
            return null;
        }
    }

    // ── File pickers ──────────────────────────────────────────────────────────

    private async void OnBrowseCsprojClicked(object sender, EventArgs e)
    {
        string? rel = await PickFileRelativeToProjectAsync(".csproj").ConfigureAwait(true);
        if (rel is not null) GameAppCsprojEntry.Text = rel;
    }

    private async void OnBrowseScriptsCsprojClicked(object sender, EventArgs e)
    {
        string? rel = await PickFileRelativeToProjectAsync(".csproj").ConfigureAwait(true);
        if (rel is not null) GameScriptsCsprojEntry.Text = rel;
    }

    private async Task<string?> PickFileRelativeToProjectAsync(string extension)
    {
        try
        {
            Microsoft.UI.Xaml.Window? win = Application.Current?.Windows.FirstOrDefault()
                ?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (win is null) return null;

            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(win);
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(extension);

            Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();
            if (file is null) return null;

            string fullPath = file.Path;
            if (!fullPath.StartsWith(_project.RootPath, StringComparison.OrdinalIgnoreCase))
                return null;

            return Path.GetRelativePath(_project.RootPath, fullPath);
        }
        catch
        {
            return null;
        }
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
        await _settings.SaveAsync(_project).ConfigureAwait(true);
        _tcs.TrySetResult(true);
        await Navigation.PopModalAsync();
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
