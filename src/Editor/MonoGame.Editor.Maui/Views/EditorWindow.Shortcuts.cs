namespace MonoGame.Editor.Maui.Views;

public sealed partial class EditorWindow
{
    #region Keyboard shortcuts

    private void OnNativeKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        Microsoft.UI.Xaml.Window? win = Application.Current?.Windows.FirstOrDefault()
            ?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

        bool textFocused = win?.Content?.XamlRoot is { } root &&
            Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(root)
                is Microsoft.UI.Xaml.Controls.TextBox or Microsoft.UI.Xaml.Controls.PasswordBox;

        bool ctrl = Microsoft.UI.Input.InputKeyboardSource
            .GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        bool shift = Microsoft.UI.Input.InputKeyboardSource
            .GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        bool alt = Microsoft.UI.Input.InputKeyboardSource
            .GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        EditorFocusContext focus = EditorContext.Instance.ActiveFocus;

        switch (e.Key)
        {
            // Global shortcuts — always active
            case Windows.System.VirtualKey.Z when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _vm.UndoCommand.Execute(null));
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.Y when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _vm.RedoCommand.Execute(null));
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.S when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _ = _vm.SaveSceneAsync());
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.S when ctrl && shift:
                MainThread.BeginInvokeOnMainThread(() => _ = _vm.SaveSceneAsAsync());
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.B when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _ = _vm.BuildSolutionAsync());
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.F5 when ctrl:
                MainThread.BeginInvokeOnMainThread(_vm.Play);
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.G when ctrl && !shift:
                MainThread.BeginInvokeOnMainThread(() => _ = _vm.GenerateCodeAsync());
                e.Handled = true;
                return;
        }

        // Menu mnemonics (Alt+letter) — only when no specific panel holds focus.
        if (alt && focus is EditorFocusContext.Global)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.F:
                    MainThread.BeginInvokeOnMainThread(() => OnFileMenuClicked(this, EventArgs.Empty));
                    e.Handled = true;
                    return;
                case Windows.System.VirtualKey.E:
                    MainThread.BeginInvokeOnMainThread(() => OnEditMenuClicked(this, EventArgs.Empty));
                    e.Handled = true;
                    return;
                case Windows.System.VirtualKey.P:
                    MainThread.BeginInvokeOnMainThread(() => OnProjectMenuClicked(this, EventArgs.Empty));
                    e.Handled = true;
                    return;
                case Windows.System.VirtualKey.D:
                    MainThread.BeginInvokeOnMainThread(() => OnDebugMenuClicked(this, EventArgs.Empty));
                    e.Handled = true;
                    return;
                case Windows.System.VirtualKey.V:
                    MainThread.BeginInvokeOnMainThread(() => OnViewMenuClicked(this, EventArgs.Empty));
                    e.Handled = true;
                    return;
            }
        }

        // Viewport shortcuts — only when the viewport holds focus and no text input is focused.
        if (textFocused || focus is not EditorFocusContext.Viewport) return;

        switch (e.Key)
        {
            case Windows.System.VirtualKey.Q:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool("Select"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.W:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool("Move"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.E:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool("Rotate"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.R:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool("Scale"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.T:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool("Rect"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.H:
                MainThread.BeginInvokeOnMainThread(() => _vm.ActivateTool("Pan"));
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.G:
                MainThread.BeginInvokeOnMainThread(_vm.ToggleSnap);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Delete:
                MainThread.BeginInvokeOnMainThread(_vm.DeleteSelected);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.F:
                MainThread.BeginInvokeOnMainThread(FocusOnSelected);
                e.Handled = true;
                break;
        }
    }

    #endregion
}
