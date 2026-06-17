using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Infrastructure;

/// <summary>
/// Servicio estático de diálogos modales para WinForms (port de DialogService de MAUI).
/// Todos los métodos deben invocarse desde el hilo UI.
/// </summary>
internal static class DialogService
{
    // ── Alertas y confirmaciones ──────────────────────────────────────────────

    /// <summary>Muestra un mensaje informativo.</summary>
    public static Task AlertAsync(string title, string message, string cancel = "OK")
    {
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        return Task.CompletedTask;
    }

    /// <summary>Muestra una pregunta Sí/No. Devuelve <c>true</c> si el usuario acepta.</summary>
    public static Task<bool> ConfirmAsync(
        string title,
        string message,
        string accept = "Aceptar",
        string cancel = "Cancelar")
    {
        DialogResult result = MessageBox.Show(
            message, title,
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question);

        return Task.FromResult(result == DialogResult.OK);
    }

    /// <summary>Muestra un cuadro de texto para que el usuario introduzca un valor.</summary>
    public static Task<string?> PromptAsync(
        string title,
        string message,
        string initialValue = "",
        int maxLength = 255)
    {
        using PromptForm form = new(title, message, initialValue, maxLength);
        DialogResult result = form.ShowDialog(ActiveWindow());
        return Task.FromResult(result == DialogResult.OK ? form.Value : null);
    }

    /// <summary>Muestra una hoja de acciones (menú de botones). Devuelve el botón pulsado.</summary>
    public static Task<string?> ActionSheetAsync(
        string title,
        string? cancel,
        string? destructive,
        params string[] buttons)
    {
        using ActionSheetForm form = new(title, cancel, destructive, buttons);
        form.ShowDialog(ActiveWindow());
        return Task.FromResult(form.SelectedAction);
    }

    // ── Selectores de fichero y carpeta ───────────────────────────────────────

    /// <summary>Abre un diálogo de selección de fichero.</summary>
    public static Task<string?> PickFileAsync(string title = "Seleccionar fichero", string filter = "Todos los ficheros (*.*)|*.*")
    {
        using OpenFileDialog dlg = new()
        {
            Title  = title,
            Filter = filter,
        };

        return Task.FromResult(
            dlg.ShowDialog(ActiveWindow()) == DialogResult.OK ? dlg.FileName : null);
    }

    /// <summary>Abre un diálogo de guardado de fichero.</summary>
    public static Task<string?> SaveFileAsync(
        string suggestedName = "",
        string title = "Guardar fichero",
        string filter = "Todos los ficheros (*.*)|*.*")
    {
        using SaveFileDialog dlg = new()
        {
            Title    = title,
            FileName = suggestedName,
            Filter   = filter,
        };

        return Task.FromResult(
            dlg.ShowDialog(ActiveWindow()) == DialogResult.OK ? dlg.FileName : null);
    }

    /// <summary>Abre un diálogo de selección de carpeta.</summary>
    public static Task<string?> PickFolderAsync(string title = "Seleccionar carpeta")
    {
        using FolderBrowserDialog dlg = new()
        {
            Description         = title,
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
        };

        return Task.FromResult(
            dlg.ShowDialog(ActiveWindow()) == DialogResult.OK ? dlg.SelectedPath : null);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IWin32Window? ActiveWindow() =>
        Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;

    // ── Formularios internos ──────────────────────────────────────────────────

    /// <summary>Diálogo de prompt de texto simple.</summary>
    private sealed class PromptForm : Form
    {
        private readonly TextBox _textBox;
        public string Value => _textBox.Text;

        public PromptForm(string title, string message, string initialValue, int maxLength)
        {
            Text            = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false;
            MinimizeBox     = false;
            ClientSize      = new System.Drawing.Size(360, 130);

            Label lblMessage = new()
            {
                Text     = message,
                Location = new System.Drawing.Point(12, 12),
                Size     = new System.Drawing.Size(336, 20),
            };

            _textBox = new TextBox
            {
                Text      = initialValue,
                MaxLength  = maxLength,
                Location  = new System.Drawing.Point(12, 38),
                Size      = new System.Drawing.Size(336, 23),
            };

            Button btnOk = new()
            {
                Text         = "Aceptar",
                DialogResult = DialogResult.OK,
                Location     = new System.Drawing.Point(192, 90),
                Size         = new System.Drawing.Size(75, 28),
            };

            Button btnCancel = new()
            {
                Text         = "Cancelar",
                DialogResult = DialogResult.Cancel,
                Location     = new System.Drawing.Point(273, 90),
                Size         = new System.Drawing.Size(75, 28),
            };

            AcceptButton = btnOk;
            CancelButton = btnCancel;
            Controls.AddRange([lblMessage, _textBox, btnOk, btnCancel]);
        }
    }

    /// <summary>Diálogo de hoja de acciones con lista de botones.</summary>
    private sealed class ActionSheetForm : Form
    {
        public string? SelectedAction { get; private set; }

        public ActionSheetForm(string title, string? cancel, string? destructive, string[] buttons)
        {
            Text            = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = false;
            MinimizeBox     = false;

            int y = 12;
            foreach (string btn in buttons)
            {
                string captured = btn;
                Button b = new()
                {
                    Text     = captured,
                    Location = new System.Drawing.Point(12, y),
                    Size     = new System.Drawing.Size(260, 30),
                };
                b.Click += (_, _) =>
                {
                    SelectedAction = captured;
                    DialogResult   = DialogResult.OK;
                    Close();
                };
                Controls.Add(b);
                y += 36;
            }

            if (cancel is not null)
            {
                Button btnCancel = new()
                {
                    Text         = cancel,
                    DialogResult = DialogResult.Cancel,
                    Location     = new System.Drawing.Point(12, y),
                    Size         = new System.Drawing.Size(260, 30),
                };
                Controls.Add(btnCancel);
                y += 36;
            }

            ClientSize = new System.Drawing.Size(284, y + 8);
        }
    }
}
