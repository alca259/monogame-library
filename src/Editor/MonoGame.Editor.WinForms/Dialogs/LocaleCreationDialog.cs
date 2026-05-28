using System.Text.RegularExpressions;

namespace MonoGame.Editor.WinForms.Dialogs;

/// <summary>
/// Modal dialog that creates a new empty locale JSON file in <c>src/GameApp/i18n/</c>.
/// </summary>
public sealed class LocaleCreationDialog : Form
{
    private static readonly Regex _localeRegex = new(
        @"^[a-z]{2,3}(-[A-Z]{2,4})?$", RegexOptions.Compiled);

    private readonly TextBox _localeBox;
    private readonly Label   _validationLabel;
    private readonly Button  _createButton;
    private readonly Button  _cancelButton;

    private readonly string _translationsRoot;

    /// <summary>Creates the dialog targeting the given <c>src/GameApp/i18n/</c> path.</summary>
    public LocaleCreationDialog(string translationsRoot)
    {
        _translationsRoot = translationsRoot;

        Text            = "New Locale";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        ClientSize      = new System.Drawing.Size(340, 130);
        MaximizeBox     = false;
        MinimizeBox     = false;
        Font            = new System.Drawing.Font("Segoe UI", 9f);

        TableLayoutPanel layout = new()
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 3,
            Padding     = new Padding(8),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));

        layout.Controls.Add(MakeLabel("Locale code:"), 0, 0);
        _localeBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "e.g. en-US" };
        _localeBox.TextChanged += (_, _) => UpdateValidation();
        layout.Controls.Add(_localeBox, 1, 0);

        _validationLabel = new Label
        {
            Dock      = DockStyle.Fill,
            ForeColor = System.Drawing.Color.OrangeRed,
            Text      = string.Empty,
        };
        layout.SetColumnSpan(_validationLabel, 2);
        layout.Controls.Add(_validationLabel, 0, 1);

        FlowLayoutPanel buttons = new()
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding       = new Padding(0, 4, 0, 0),
        };
        _cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };
        _createButton = new Button { Text = "Create", Width = 80, Enabled = false };
        _createButton.Click += OnCreate;
        buttons.Controls.Add(_cancelButton);
        buttons.Controls.Add(_createButton);
        layout.SetColumnSpan(buttons, 2);
        layout.Controls.Add(buttons, 0, 2);

        Controls.Add(layout);
        AcceptButton = _createButton;
        CancelButton = _cancelButton;
    }

    private void OnCreate(object? sender, EventArgs e)
    {
        string locale = _localeBox.Text.Trim();

        Directory.CreateDirectory(_translationsRoot);
        string filePath = Path.Combine(_translationsRoot, $"{locale}.json");
        if (!File.Exists(filePath))
            File.WriteAllText(filePath, "{}\n");

        DialogResult = DialogResult.OK;
    }

    private void UpdateValidation()
    {
        string code  = _localeBox.Text.Trim();
        bool   valid = _localeRegex.IsMatch(code);
        _createButton.Enabled = valid;
        _validationLabel.Text = valid || code.Length == 0
            ? string.Empty
            : "Use format: xx or xx-XX (e.g. en, es-ES, zh-CN).";
    }

    private static Label MakeLabel(string text) => new()
    {
        Text      = text,
        Dock      = DockStyle.Fill,
        TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
    };
}
