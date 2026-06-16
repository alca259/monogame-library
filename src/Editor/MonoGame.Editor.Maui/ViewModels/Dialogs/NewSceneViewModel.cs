using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MonoGame.Editor.Maui.ViewModels.Dialogs;

/// <summary>ViewModel del diálogo "New Scene": nombre y tamaño del mundo.</summary>
public sealed partial class NewSceneViewModel : DialogViewModel<NewSceneResult>
{
    [ObservableProperty]
    private string _sceneName = string.Empty;

    [ObservableProperty]
    private string _worldWidth = "1920";

    [ObservableProperty]
    private string _worldHeight = "1080";

    [RelayCommand]
    private void Submit()
    {
        string name = SceneName?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(name))
        {
            ShowError("Scene name is required.");
            return;
        }

        if (!float.TryParse(WorldWidth, out float width) || width <= 0f)
        {
            ShowError("World width must be a positive number.");
            return;
        }

        if (!float.TryParse(WorldHeight, out float height) || height <= 0f)
        {
            ShowError("World height must be a positive number.");
            return;
        }

        Close(new NewSceneResult(name, width, height));
    }
}
