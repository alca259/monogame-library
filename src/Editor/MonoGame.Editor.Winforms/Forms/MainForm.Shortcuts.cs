using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Forms;

partial class MainForm
{
    /// <summary>
    /// Enruta atajos de teclado según el foco activo del editor:
    /// - Globales (Ctrl+S/Z/Y/B/F5): siempre activos.
    /// - Viewport (Q/W/E/R/T/H/U/Delete/F): solo cuando el viewport tiene el foco.
    /// </summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // ── Globales ─────────────────────────────────────────────────────────
        switch (keyData)
        {
            case Keys.Control | Keys.S:
                _vm.SaveSceneCommand.Execute(null);
                return true;

            case Keys.Control | Keys.Z:
                _vm.UndoCommand.Execute(null);
                return true;

            case Keys.Control | Keys.Y:
                _vm.RedoCommand.Execute(null);
                return true;

            case Keys.Control | Keys.B:
                _vm.BuildContentCommand.Execute(null);
                return true;

            case Keys.F5:
                _vm.PlayCommand.Execute(null);
                return true;

            case Keys.Control | Keys.A:
                _vm.SelectAllCommand.Execute(null);
                return true;

            case Keys.Control | Keys.X:
                _vm.CutCommand.Execute(null);
                return true;

            case Keys.Control | Keys.C:
                _vm.CopyCommand.Execute(null);
                return true;

            case Keys.Control | Keys.V:
                _vm.PasteCommand.Execute(null);
                return true;
        }

        // ── Viewport (solo cuando el viewport está enfocado) ──────────────
        bool viewportFocused = _vm.IsViewportFocused;
        if (viewportFocused)
        {
            switch (keyData)
            {
                case Keys.Q:
                    _vm.ActivateToolCommand.Execute(EditorWindowViewModel.SceneTools.Select);
                    return true;
                case Keys.W:
                    _vm.ActivateToolCommand.Execute(EditorWindowViewModel.SceneTools.Move);
                    return true;
                case Keys.E:
                    _vm.ActivateToolCommand.Execute(EditorWindowViewModel.SceneTools.Rotate);
                    return true;
                case Keys.R:
                    _vm.ActivateToolCommand.Execute(EditorWindowViewModel.SceneTools.Scale);
                    return true;
                case Keys.T:
                    _vm.ActivateToolCommand.Execute(EditorWindowViewModel.SceneTools.Rect);
                    return true;
                case Keys.H:
                    _vm.ToggleNavCommand.Execute(null);
                    return true;
                case Keys.U:
                    _vm.ToggleResCommand.Execute(null);
                    return true;
                case Keys.G:
                    _vm.ToggleSnapCommand.Execute(null);
                    return true;
                case Keys.Delete:
                    _vm.DeleteSelectedCommand.Execute(null);
                    return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
