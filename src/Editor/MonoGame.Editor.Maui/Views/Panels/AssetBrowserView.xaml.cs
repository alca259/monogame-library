namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Pestaña Assets del dock inferior. La lógica vive en <see cref="AssetBrowserViewModel"/>;
/// el code-behind enlaza la VM, la expone como <see cref="Vm"/> para los menús contextuales
/// de las plantillas, y construye el breadcrumb (UI dinámica) al recibir
/// <see cref="AssetBrowserViewModel.FolderChanged"/>.
/// </summary>
public sealed partial class AssetBrowserView : ContentView
{
    private readonly AssetBrowserViewModel _vm = new();

    /// <summary>VM tipada, referenciada desde los <c>MenuFlyout</c> de las plantillas.</summary>
    public AssetBrowserViewModel Vm => _vm;

    public AssetBrowserView()
    {
        InitializeComponent();
        BindingContext = _vm;
        _vm.FolderChanged += BuildBreadcrumb;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) _vm.Attach();
        else _vm.Detach();
    }

    // ── Breadcrumb (UI dinámica dependiente de la ruta actual) ──────────────────

    private void BuildBreadcrumb()
    {
        BreadcrumbLayout.Children.Clear();

        string contentRoot = _vm.ContentRoot;
        string folderPath  = _vm.CurrentFolderPath;
        if (string.IsNullOrEmpty(contentRoot)) return;

        var segments = new List<(string Label, string Path)> { ("Content", contentRoot) };

        string relative = Path.GetRelativePath(contentRoot, folderPath);
        if (relative != ".")
        {
            string accumulated = contentRoot;
            foreach (string part in relative.Split(Path.DirectorySeparatorChar))
            {
                accumulated = Path.Combine(accumulated, part);
                segments.Add((part, accumulated));
            }
        }

        for (int i = 0; i < segments.Count; i++)
        {
            if (i > 0)
                BreadcrumbLayout.Children.Add(new Label
                {
                    Text            = " › ",
                    Style           = (Style)Application.Current!.Resources["DimLabel"],
                    VerticalOptions = LayoutOptions.Center,
                });

            (string segLabel, string segPath) = segments[i];
            bool isLast = i == segments.Count - 1;

            if (isLast)
            {
                BreadcrumbLayout.Children.Add(new Label
                {
                    Text            = segLabel,
                    Style           = (Style)Application.Current!.Resources["PrimaryLabel"],
                    VerticalOptions = LayoutOptions.Center,
                    Padding         = new Thickness(4, 0),
                });
            }
            else
            {
                string captured = segPath;
                Button btn = new()
                {
                    Text            = segLabel,
                    FontSize        = 11,
                    Padding         = new Thickness(4, 0),
                    HeightRequest   = 22,
                    BackgroundColor = Colors.Transparent,
                    TextColor       = Color.FromArgb("#9A9AA2"),
                    VerticalOptions = LayoutOptions.Center,
                };
                btn.Clicked += (_, _) => _vm.NavigateToFolderCommand.Execute(captured);
                BreadcrumbLayout.Children.Add(btn);
            }
        }
    }
}
