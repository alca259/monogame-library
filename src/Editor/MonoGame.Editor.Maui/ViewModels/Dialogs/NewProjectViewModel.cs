using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonoGame.Editor.Maui.Views.Dialogs;

namespace MonoGame.Editor.Maui.ViewModels.Dialogs;

/// <summary>ViewModel del diálogo "New Project": nombre, carpeta padre y .csproj del juego.</summary>
public sealed partial class NewProjectViewModel : DialogViewModel<NewProjectResult>
{
    [ObservableProperty]
    private string _projectName = string.Empty;

    [ObservableProperty]
    private string _parentPath = string.Empty;

    [ObservableProperty]
    private string _gameCsproj = string.Empty;

    [RelayCommand]
    private async Task BrowseParentAsync()
    {
        string? picked = await DialogService.PickFolderAsync();
        if (picked is not null) ParentPath = picked;
    }

    [RelayCommand]
    private async Task BrowseCsprojAsync()
    {
        string? picked = await DialogService.PickFileAsync(new PickOptions { PickerTitle = "Select .csproj file" });
        if (picked is not null) GameCsproj = picked;
    }

    [RelayCommand]
    private void Submit()
    {
        string name   = ProjectName?.Trim() ?? string.Empty;
        string parent = ParentPath?.Trim()  ?? string.Empty;
        string csproj = GameCsproj?.Trim()   ?? string.Empty;

        if (string.IsNullOrEmpty(name))
        {
            ShowError("Project name is required.");
            return;
        }

        if (string.IsNullOrEmpty(parent))
        {
            ShowError("Parent folder is required.");
            return;
        }

        Close(new NewProjectResult(name, parent, csproj));
    }
}
