namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Dock inferior: barra de 9 pestañas con su contenido correspondiente.
/// Fase 0: Assets, Console y Scenes funcionales. Resto son placeholders (Fase 7).
/// </summary>
public sealed partial class DockBarView : ContentView
{
    private static readonly Color ActiveTabFg   = Color.FromArgb("#E6E6E8");
    private static readonly Color InactiveTabFg = Color.FromArgb("#9A9AA2");

    private string _activeTab = "Assets";

    public DockBarView()
    {
        InitializeComponent();
        SetActiveTab(AssetsTabBtn);
    }

    private void OnDockTabClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        _activeTab = btn.CommandParameter as string ?? "Assets";
        SetActiveTab(btn);
        UpdateTabContent();
    }

    private void SetActiveTab(Button active)
    {
        foreach (Button btn in new[]
        {
            AssetsTabBtn, ConsoleTabBtn, ScenesTabBtn, LocalizationTabBtn,
            InputMapsTabBtn, TilemapTabBtn, HistoryTabBtn, ScriptsTabBtn
        })
            btn.TextColor = btn == active ? ActiveTabFg : InactiveTabFg;
    }

    private void UpdateTabContent()
    {
        AssetsContent.IsVisible       = _activeTab == "Assets";
        ConsoleContent.IsVisible      = _activeTab == "Console";
        ScenesContent.IsVisible       = _activeTab == "Scenes";
        LocalizationContent.IsVisible = _activeTab == "Localization";
        InputMapsContent.IsVisible    = _activeTab == "InputMaps";
        TilemapContent.IsVisible      = _activeTab == "Tilemap";
        HistoryContent.IsVisible      = _activeTab == "History";
        ScriptsContent.IsVisible      = _activeTab == "Scripts";
    }
}
