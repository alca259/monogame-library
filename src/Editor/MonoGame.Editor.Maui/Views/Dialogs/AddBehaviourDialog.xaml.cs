using System.Collections.ObjectModel;

namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed partial class AddBehaviourDialog : ContentPage
{
    private static readonly Color PendingColor = Color.FromArgb("#6A6A72");

    private readonly TaskCompletionSource<string?> _tcs = new();

    private readonly List<string> _allEntries      = [];
    private readonly ObservableCollection<string> _filtered = [];

    private string? _selectedEntry;

    private AddBehaviourDialog() => InitializeComponent();

    public static async Task<string?> ShowAsync(INavigation navigation,
                                                 GameObjectRegistry registry)
    {
        var dialog = new AddBehaviourDialog();
        dialog.Populate(registry);
        await navigation.PushModalAsync(dialog);
        return await dialog._tcs.Task;
    }

    private void Populate(GameObjectRegistry registry)
    {
        foreach (string key in registry.RegisteredTypes.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            _allEntries.Add(key);

        foreach (string pending in registry.PendingTypeNames.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            _allEntries.Add($"~ {pending}");

        TypeList.ItemsSource = _filtered;
        RefreshList(string.Empty);
    }

    private void RefreshList(string filter)
    {
        _filtered.Clear();
        foreach (string entry in _allEntries)
        {
            if (string.IsNullOrEmpty(filter) ||
                entry.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                _filtered.Add(entry);
            }
        }
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
        => RefreshList(e.NewTextValue ?? string.Empty);

    private void OnTypeSelected(object sender, SelectionChangedEventArgs e)
    {
        _selectedEntry = e.CurrentSelection.FirstOrDefault() as string;
        AddButton.IsEnabled = _selectedEntry is not null;
    }

    protected override bool OnBackButtonPressed()
    {
        _tcs.TrySetResult(null);
        return base.OnBackButtonPressed();
    }

    private void OnCancel(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        _ = Navigation.PopModalAsync();
    }

    private void OnSubmit(object sender, EventArgs e)
    {
        if (_selectedEntry is null) return;

        // Strip pending prefix before returning
        string typeName = _selectedEntry.StartsWith("~ ", StringComparison.Ordinal)
            ? _selectedEntry[2..]
            : _selectedEntry;

        _tcs.TrySetResult(typeName);
        _ = Navigation.PopModalAsync();
    }
}
