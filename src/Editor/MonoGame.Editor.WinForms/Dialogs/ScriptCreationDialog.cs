using System.Text.RegularExpressions;

namespace MonoGame.Editor.WinForms.Dialogs;

/// <summary>
/// Diálogo modal que crea un stub de <c>GameBehaviour</c> en <c>src/GameScripts/</c>.
/// </summary>
public sealed class ScriptCreationDialog : Form
{
    private static readonly Regex _identifierRegex = new(
        @"^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);

    private readonly TextBox _classNameBox;
    private readonly TextBox _namespaceBox;
    private readonly Label   _validationLabel;
    private readonly Button  _createButton;
    private readonly Button  _cancelButton;

    private readonly string _scriptsRoot;

    /// <summary>Crea el diálogo dirigido a la ruta <c>src/GameScripts/</c> indicada.</summary>
    public ScriptCreationDialog(string scriptsRoot, string defaultNamespace = "")
    {
        _scriptsRoot = scriptsRoot;

        Text            = "New Script";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        ClientSize      = new System.Drawing.Size(380, 170);
        MaximizeBox     = false;
        MinimizeBox     = false;
        Font            = new System.Drawing.Font("Segoe UI", 9f);

        TableLayoutPanel layout = new()
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 4,
            Padding     = new Padding(8),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));

        layout.Controls.Add(MakeLabel("Class name:"), 0, 0);
        _classNameBox = new TextBox { Dock = DockStyle.Fill };
        _classNameBox.TextChanged += (_, _) => UpdateValidation();
        layout.Controls.Add(_classNameBox, 1, 0);

        layout.Controls.Add(MakeLabel("Namespace:"), 0, 1);
        _namespaceBox = new TextBox { Dock = DockStyle.Fill, Text = defaultNamespace };
        layout.Controls.Add(_namespaceBox, 1, 1);

        _validationLabel = new Label
        {
            Dock      = DockStyle.Fill,
            ForeColor = System.Drawing.Color.OrangeRed,
            Text      = string.Empty,
        };
        layout.SetColumnSpan(_validationLabel, 2);
        layout.Controls.Add(_validationLabel, 0, 2);

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
        layout.Controls.Add(buttons, 0, 3);

        Controls.Add(layout);
        AcceptButton = _createButton;
        CancelButton = _cancelButton;
    }

    private void OnCreate(object? sender, EventArgs e)
    {
        string className = _classNameBox.Text.Trim();
        string ns        = _namespaceBox.Text.Trim();

        Directory.CreateDirectory(_scriptsRoot);
        string filePath = Path.Combine(_scriptsRoot, $"{className}.cs");
        if (!File.Exists(filePath))
            File.WriteAllText(filePath, BuildStub(className, ns));

        DialogResult = DialogResult.OK;
    }

    private static string BuildStub(string className, string ns)
    {
        string nsLine = string.IsNullOrEmpty(ns) ? string.Empty : $"namespace {ns};\n\n";
        return nsLine +
               $"public sealed class {className} : GameBehaviour\n" +
               "{\n" +
               "    public override void Start()  { }\n" +
               "    public override void Update() { }\n" +
               "}\n";
    }

    private void UpdateValidation()
    {
        string name  = _classNameBox.Text.Trim();
        bool   valid = _identifierRegex.IsMatch(name);
        _createButton.Enabled = valid;
        _validationLabel.Text = valid || name.Length == 0
            ? string.Empty
            : "Class name must start with a letter and contain only letters, digits, or underscores.";
    }

    private static Label MakeLabel(string text) => new()
    {
        Text      = text,
        Dock      = DockStyle.Fill,
        TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
    };
}
