namespace MonoGame.Editor.Maui.Views;

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Core;
using Windows.System;

public sealed partial class EditorWindow
{
    #region Keyboard shortcuts

    private void OnNativeKeyDown(object sender, KeyRoutedEventArgs e)
    {
        Microsoft.UI.Xaml.Window? win = Application.Current?.Windows.FirstOrDefault()
            ?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

        bool textFocused = win?.Content?.XamlRoot is { } root &&
            FocusManager.GetFocusedElement(root)
                is TextBox or PasswordBox;

        bool ctrl = InputKeyboardSource
            .GetKeyStateForCurrentThread(VirtualKey.Control)
            .HasFlag(CoreVirtualKeyStates.Down);
        bool shift = InputKeyboardSource
            .GetKeyStateForCurrentThread(VirtualKey.Shift)
            .HasFlag(CoreVirtualKeyStates.Down);
        bool alt = InputKeyboardSource
            .GetKeyStateForCurrentThread(VirtualKey.Menu)
            .HasFlag(CoreVirtualKeyStates.Down);

        EditorFocusContext focus = EditorContext.Instance.ActiveFocus;

        switch (e.Key)
        {
            // Global shortcuts — always active
            case VirtualKey.Z when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _vm.UndoCommand.Execute(null));
                e.Handled = true;
                return;
            case VirtualKey.Y when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _vm.RedoCommand.Execute(null));
                e.Handled = true;
                return;
            case VirtualKey.S when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _ = _vm.SaveSceneAsync());
                e.Handled = true;
                return;
            case VirtualKey.S when ctrl && shift:
                MainThread.BeginInvokeOnMainThread(() => _ = _vm.SaveSceneAsAsync());
                e.Handled = true;
                return;
            case VirtualKey.B when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _ = _vm.BuildSolutionAsync());
                e.Handled = true;
                return;
            case VirtualKey.F5 when ctrl:
                MainThread.BeginInvokeOnMainThread(_vm.Play);
                e.Handled = true;
                return;
            case VirtualKey.G when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _ = _vm.GenerateCodeAsync());
                e.Handled = true;
                return;
        }

        // Menu mnemonics (Alt+letter) — only when no specific panel holds focus.
        if (alt && focus is EditorFocusContext.Global)
        {
            switch (e.Key)
            {
                case VirtualKey.F:
                    MainThread.BeginInvokeOnMainThread(() => OnFileMenuClicked(this, EventArgs.Empty));
                    e.Handled = true;
                    return;
                case VirtualKey.E:
                    MainThread.BeginInvokeOnMainThread(() => OnEditMenuClicked(this, EventArgs.Empty));
                    e.Handled = true;
                    return;
                case VirtualKey.P:
                    MainThread.BeginInvokeOnMainThread(() => OnProjectMenuClicked(this, EventArgs.Empty));
                    e.Handled = true;
                    return;
                case VirtualKey.D:
                    MainThread.BeginInvokeOnMainThread(() => OnDebugMenuClicked(this, EventArgs.Empty));
                    e.Handled = true;
                    return;
                case VirtualKey.V:
                    MainThread.BeginInvokeOnMainThread(() => OnViewMenuClicked(this, EventArgs.Empty));
                    e.Handled = true;
                    return;
            }
        }

        // Viewport shortcuts — only when the viewport holds focus and no text input is focused.
        if (textFocused || focus is not EditorFocusContext.Viewport) return;

        switch (e.Key)
        {
            case VirtualKey.Q:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool(EditorWindowViewModel.SceneTools.Select));
                e.Handled = true;
                break;
            case VirtualKey.W:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool(EditorWindowViewModel.SceneTools.Move));
                e.Handled = true;
                break;
            case VirtualKey.E:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool(EditorWindowViewModel.SceneTools.Rotate));
                e.Handled = true;
                break;
            case VirtualKey.R:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool(EditorWindowViewModel.SceneTools.Scale));
                e.Handled = true;
                break;
            case VirtualKey.T:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool(EditorWindowViewModel.SceneTools.Rect));
                e.Handled = true;
                break;
            case VirtualKey.H:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool(EditorWindowViewModel.SceneTools.Pan));
                e.Handled = true;
                break;
            case VirtualKey.U:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool(EditorWindowViewModel.SceneTools.Universal));
                e.Handled = true;
                break;
            case VirtualKey.G:
                MainThread.BeginInvokeOnMainThread(_vm.ToggleSnap);
                e.Handled = true;
                break;
            case VirtualKey.Delete:
                MainThread.BeginInvokeOnMainThread(_vm.DeleteSelected);
                e.Handled = true;
                break;
            case VirtualKey.F:
                MainThread.BeginInvokeOnMainThread(FocusOnSelected);
                e.Handled = true;
                break;
        }
    }

    #endregion
}
