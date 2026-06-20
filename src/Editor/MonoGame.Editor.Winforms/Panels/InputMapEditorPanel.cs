using Alca.MonoGame.Kernel.Input;
using System.Drawing;
using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Panels;

/// <summary>
/// Panel de edición de mapas de input: lista ficheros <c>*.input.json</c>,
/// acciones y bindings, con CRUD básico.
/// </summary>
internal sealed class InputMapEditorPanel : UserControl
{
    private readonly InputMapEditorViewModel _vm = new();

    private readonly ListBox _lstFiles;
    private readonly ListBox _lstActions;
    private readonly ListBox _lstBindings;

    private bool _updating;

    public InputMapEditorPanel()
    {
        SuspendLayout();
        BackColor = EditorColors.PanelBackground;
        Dock      = DockStyle.Fill;

        // ── Layout principal: tres columnas ──────────────────────────────────
        TableLayoutPanel main = new()
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 3,
            RowCount    = 1,
            BackColor   = EditorColors.Border,
        };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Panel fileCol    = BuildColumn("Input Maps", out _lstFiles);
        Panel actionCol  = BuildColumn("Actions",    out _lstActions);
        Panel bindingCol = BuildColumn("Bindings",   out _lstBindings);

        // Toolbars por columna
        AddToolbar(fileCol, null, null, "Map file — read only");

        Button btnAddAction = MakeToolButton("+");
        Button btnRemAction = MakeToolButton("−");
        AddToolbar(actionCol, btnAddAction, btnRemAction, "Actions");

        Button btnAddBind = MakeToolButton("+");
        Button btnRemBind = MakeToolButton("−");
        AddToolbar(bindingCol, btnAddBind, btnRemBind, "Bindings");

        // Save bottom bar
        Panel bottomBar = new()
        {
            Dock      = DockStyle.Bottom,
            Height    = 30,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(4),
        };
        Button btnSave = new()
        {
            Text      = "Save",
            Dock      = DockStyle.Right,
            Width     = 70,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.AccentBlue,
            ForeColor = EditorColors.TextPrimary,
            Font      = EditorFonts.PrimaryBold,
        };
        btnSave.FlatAppearance.BorderSize = 0;
        bottomBar.Controls.Add(btnSave);

        main.Controls.Add(fileCol,    0, 0);
        main.Controls.Add(actionCol,  1, 0);
        main.Controls.Add(bindingCol, 2, 0);

        Controls.Add(main);
        Controls.Add(bottomBar);

        // ── Eventos de controles ──────────────────────────────────────────────
        _lstFiles.SelectedIndexChanged += async (_, _) =>
        {
            if (_updating) return;
            await _vm.SelectFileAsync(_lstFiles.SelectedIndex).ConfigureAwait(true);
        };

        _lstActions.SelectedIndexChanged += (_, _) =>
        {
            if (_updating) return;
            _vm.SelectAction(_lstActions.SelectedItem as string);
        };

        _lstBindings.SelectedIndexChanged += (_, _) =>
        {
            if (_updating) return;
            _vm.SelectBinding(_lstBindings.SelectedItem as string);
        };

        btnAddAction.Click += (_, _) =>
        {
            if (!_vm.HasFile) return;
            string? name = WinFormsDialogService.Prompt(FindForm(), "Add Action", "Action name:");
            if (!string.IsNullOrWhiteSpace(name)) _vm.AddAction(name);
        };

        btnRemAction.Click += (_, _) =>
        {
            if (!_vm.HasAction) return;
            bool ok = WinFormsDialogService.Confirm(FindForm(), "Remove Action",
                "Remove selected action and all its bindings?", "Remove", "Cancel");
            if (ok) _vm.RemoveAction();
        };

        btnAddBind.Click += (_, _) =>
        {
            if (!_vm.HasAction) return;
            using AddBindingDialog dlg = new();
            if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                _vm.AddBinding(dlg.Device, dlg.Code);
        };

        btnRemBind.Click += (_, _) =>
        {
            if (_vm.HasBinding)
                _vm.RemoveBinding();
        };

        btnSave.Click += async (_, _) =>
            await _vm.SaveAsync().ConfigureAwait(true);

        // ── Eventos VM ────────────────────────────────────────────────────────
        _vm.FileListChanged    += OnFileListChanged;
        _vm.ActionListChanged  += OnActionListChanged;
        _vm.BindingListChanged += OnBindingListChanged;
        _vm.Attach();

        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.Detach();
        base.Dispose(disposing);
    }

    // ── Rebuild ────────────────────────────────────────────────────────────────

    private void OnFileListChanged()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(OnFileListChanged); return; }
        _updating = true;
        _lstFiles.BeginUpdate();
        _lstFiles.Items.Clear();
        foreach (string name in _vm.FileNames) _lstFiles.Items.Add(name);
        _lstFiles.EndUpdate();
        _updating = false;
    }

    private void OnActionListChanged()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(OnActionListChanged); return; }
        _updating = true;
        _lstActions.BeginUpdate();
        _lstActions.Items.Clear();
        foreach (string name in _vm.ActionItems) _lstActions.Items.Add(name);
        _lstActions.EndUpdate();
        _updating = false;
    }

    private void OnBindingListChanged()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(OnBindingListChanged); return; }
        _updating = true;
        _lstBindings.BeginUpdate();
        _lstBindings.Items.Clear();
        foreach (string s in _vm.BindingItems) _lstBindings.Items.Add(s);
        _lstBindings.EndUpdate();
        _updating = false;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Panel BuildColumn(string header, out ListBox list)
    {
        Panel col = new()
        {
            Dock      = DockStyle.Fill,
            BackColor = EditorColors.PanelBackground,
            Margin    = new Padding(0),
        };

        Label lbl = new()
        {
            Dock      = DockStyle.Top,
            Height    = 20,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
            Text      = header,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(6, 0, 0, 0),
        };

        list = new ListBox
        {
            Dock        = DockStyle.Fill,
            BackColor   = EditorColors.PanelBackground,
            ForeColor   = EditorColors.TextPrimary,
            Font        = EditorFonts.Small,
            BorderStyle = BorderStyle.None,
        };

        col.Controls.Add(list);
        col.Controls.Add(lbl);
        return col;
    }

    private static void AddToolbar(Panel col, Button? btnAdd, Button? btnRem, string _)
    {
        Panel toolbar = new()
        {
            Dock      = DockStyle.Bottom,
            Height    = 26,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(2),
        };

        if (btnAdd is not null)
        {
            btnAdd.Dock = DockStyle.Left;
            toolbar.Controls.Add(btnAdd);
        }

        if (btnRem is not null)
        {
            btnRem.Dock = DockStyle.Left;
            toolbar.Controls.Add(btnRem);
        }

        col.Controls.Add(toolbar);
    }

    private static Button MakeToolButton(string text) => new()
    {
        Text      = text,
        Width     = 28,
        FlatStyle = FlatStyle.Flat,
        BackColor = EditorColors.PanelBackgroundAlt,
        ForeColor = EditorColors.TextPrimary,
        Font      = EditorFonts.PrimaryBold,
    };
}

// ── Diálogo inline para añadir bindings ────────────────────────────────────────

/// <summary>Diálogo mínimo para seleccionar DeviceType + código de tecla/botón.</summary>
internal sealed class AddBindingDialog : Form
{
    public DeviceType Device { get; private set; }
    public int        Code   { get; private set; }

    private readonly ComboBox       _cmbDevice;
    private readonly NumericUpDown  _numCode;

    public AddBindingDialog()
    {
        Text            = "Add Binding";
        Width           = 280;
        Height          = 150;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterParent;
        BackColor       = EditorColors.PanelBackground;
        ForeColor       = EditorColors.TextPrimary;

        TableLayoutPanel grid = new()
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 3,
            Padding     = new Padding(8),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _cmbDevice = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font          = EditorFonts.Small,
            BackColor     = EditorColors.InputBackground,
            ForeColor     = EditorColors.TextPrimary,
        };
        foreach (DeviceType d in Enum.GetValues<DeviceType>())
            _cmbDevice.Items.Add(d);
        _cmbDevice.SelectedIndex = 0;

        _numCode = new NumericUpDown
        {
            Minimum   = 0,
            Maximum   = 512,
            Font      = EditorFonts.Small,
            BackColor = EditorColors.InputBackground,
            ForeColor = EditorColors.TextPrimary,
        };

        Button btnOk     = new() { Text = "OK",     DialogResult = DialogResult.OK,     Width = 70, FlatStyle = FlatStyle.Flat, BackColor = EditorColors.AccentBlue, ForeColor = EditorColors.TextPrimary };
        Button btnCancel = new() { Text = "Cancel",  DialogResult = DialogResult.Cancel, Width = 70, FlatStyle = FlatStyle.Flat, BackColor = EditorColors.PanelBackgroundAlt, ForeColor = EditorColors.TextSecondary };
        btnOk.FlatAppearance.BorderSize = 0;
        btnCancel.FlatAppearance.BorderColor = EditorColors.Border;

        Panel btnPanel = new() { Dock = DockStyle.Bottom, Height = 34, BackColor = EditorColors.PanelBackground, Padding = new Padding(4) };
        btnPanel.Controls.Add(btnOk);
        btnPanel.Controls.Add(btnCancel);
        btnOk.Dock = DockStyle.Right;
        btnCancel.Dock = DockStyle.Right;

        grid.Controls.Add(MakeLabel("Device"), 0, 0);
        grid.Controls.Add(_cmbDevice,          1, 0);
        grid.Controls.Add(MakeLabel("Code"),   0, 1);
        grid.Controls.Add(_numCode,            1, 1);

        Controls.Add(grid);
        Controls.Add(btnPanel);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        if (DialogResult == DialogResult.OK)
        {
            Device = (DeviceType)(_cmbDevice.SelectedItem ?? DeviceType.Keyboard);
            Code   = (int)_numCode.Value;
        }
    }

    private static Label MakeLabel(string text) => new()
    {
        Text      = text,
        ForeColor = EditorColors.TextSecondary,
        Font      = EditorFonts.Small,
        TextAlign = ContentAlignment.MiddleLeft,
        Dock      = DockStyle.Fill,
    };
}
