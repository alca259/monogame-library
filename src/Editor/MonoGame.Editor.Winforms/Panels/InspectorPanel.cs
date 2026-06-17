using System.Drawing;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Controls;
using MonoGame.Editor.Winforms.Forms.Dialogs;
using MonoGame.Editor.Winforms.Theme;
using MonoGame.Editor.Winforms.ViewModels.Panels;

namespace MonoGame.Editor.Winforms.Panels;

/// <summary>
/// Panel Inspector: muestra y edita el Transform del objeto seleccionado.
/// Fase 4 implementa Transform (Position/Rotation/Scale) y Active.
/// Las tarjetas de Behaviour se añaden en Fase 5.
/// </summary>
internal sealed class InspectorPanel : UserControl
{
    private readonly InspectorViewModel _vm = new();

    // ── Controles de cabecera ─────────────────────────────────────────────────
    private readonly Label    _lblName;
    private readonly Label    _lblId;
    private readonly CheckBox _chkActive;

    // ── Steppers de Transform (3×3) ───────────────────────────────────────────
    private readonly AxisStepper _spPosX, _spPosY, _spPosZ;
    private readonly AxisStepper _spRotX, _spRotY, _spRotZ;
    private readonly AxisStepper _spScaX, _spScaY, _spScaZ;

    // ── Placeholder de behaviours (Fase 5) ────────────────────────────────────
    private readonly Panel  _behaviourArea;
    private readonly Button _btnAddBehaviour;

    // ── Constructor ───────────────────────────────────────────────────────────

    public InspectorPanel()
    {
        SuspendLayout();

        // ── TabControl ────────────────────────────────────────────────────────
        TabControl tabs = new()
        {
            Dock      = DockStyle.Fill,
            BackColor = EditorColors.PanelBackground,
            Font      = EditorFonts.Primary,
        };
        ApplyTabStyle(tabs);

        TabPage inspectorTab = new("Inspector")
        {
            BackColor = EditorColors.PanelBackground,
            ForeColor = EditorColors.TextPrimary,
        };

        // ── Panel con scroll ──────────────────────────────────────────────────
        Panel scroll = new()
        {
            Dock        = DockStyle.Fill,
            AutoScroll  = true,
            BackColor   = EditorColors.PanelBackground,
        };

        // ── Cabecera: nombre + id + active ────────────────────────────────────
        Panel header = new()
        {
            Dock      = DockStyle.Top,
            Height    = 52,
            BackColor = EditorColors.PanelBackgroundAlt,
            Padding   = new Padding(6, 4, 6, 4),
        };

        _lblName = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 22,
            ForeColor = EditorColors.TextPrimary,
            Font      = EditorFonts.PrimaryBold,
            TextAlign = ContentAlignment.MiddleLeft,
            Text      = "—",
        };

        Panel idRow = new()
        {
            Dock   = DockStyle.Top,
            Height = 20,
        };

        _lblId = new Label
        {
            Dock      = DockStyle.Fill,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
            TextAlign = ContentAlignment.MiddleLeft,
            Text      = string.Empty,
        };

        _chkActive = new CheckBox
        {
            Dock      = DockStyle.Right,
            Width     = 60,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Small,
            Text      = "Active",
            CheckAlign = ContentAlignment.MiddleRight,
            Checked   = true,
        };

        idRow.Controls.Add(_lblId);
        idRow.Controls.Add(_chkActive);

        header.Controls.Add(idRow);
        header.Controls.Add(_lblName);

        // ── Sección Transform ─────────────────────────────────────────────────
        CollapsibleSection transformSection = new() { Title = "Transform" };
        Panel transformContent = transformSection.ContentPanel;
        transformContent.Height = 90;

        _spPosX = MakeStepper("X"); _spPosY = MakeStepper("Y"); _spPosZ = MakeStepper("Z");
        _spRotX = MakeStepper("X"); _spRotY = MakeStepper("Y"); _spRotZ = MakeStepper("Z");
        _spScaX = MakeStepper("X"); _spScaY = MakeStepper("Y"); _spScaZ = MakeStepper("Z");

        TableLayoutPanel transformGrid = BuildTransformGrid();
        transformGrid.Dock = DockStyle.Fill;
        transformContent.Controls.Add(transformGrid);

        // ── Placeholder behaviours (Fase 5) ───────────────────────────────────
        _behaviourArea = new Panel
        {
            Dock      = DockStyle.Top,
            AutoSize  = true,
            BackColor = EditorColors.PanelBackground,
        };

        _btnAddBehaviour = new Button
        {
            Text      = "+ Add Behaviour",
            Dock      = DockStyle.Top,
            Height    = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Primary,
            Enabled   = false,
        };
        _btnAddBehaviour.FlatAppearance.BorderColor = EditorColors.Border;

        // ── Ensamblaje ────────────────────────────────────────────────────────
        Panel container = new()
        {
            Dock      = DockStyle.Top,
            AutoSize  = true,
            BackColor = EditorColors.PanelBackground,
        };

        // WinForms Dock:Top apila de abajo hacia arriba, así que añadimos en orden inverso
        container.Controls.Add(_btnAddBehaviour);
        container.Controls.Add(_behaviourArea);
        container.Controls.Add(transformSection);
        container.Controls.Add(header);

        scroll.Controls.Add(container);
        inspectorTab.Controls.Add(scroll);
        tabs.TabPages.Add(inspectorTab);

        BackColor = EditorColors.PanelBackground;
        Dock      = DockStyle.Fill;
        Controls.Add(tabs);

        // ── Eventos ───────────────────────────────────────────────────────────
        _chkActive.CheckedChanged += (_, _) => _vm.SetActive(_chkActive.Checked);

        _spPosX.ValueCommitted += (_, _) => _vm.ApplyPosX(_spPosX.Value);
        _spPosY.ValueCommitted += (_, _) => _vm.ApplyPosY(_spPosY.Value);
        _spPosZ.ValueCommitted += (_, _) => _vm.ApplyPosZ(_spPosZ.Value);
        _spRotX.ValueCommitted += (_, _) => _vm.ApplyRotX(_spRotX.Value);
        _spRotY.ValueCommitted += (_, _) => _vm.ApplyRotY(_spRotY.Value);
        _spRotZ.ValueCommitted += (_, _) => _vm.ApplyRotZ(_spRotZ.Value);
        _spScaX.ValueCommitted += (_, _) => _vm.ApplyScaleX(_spScaX.Value);
        _spScaY.ValueCommitted += (_, _) => _vm.ApplyScaleY(_spScaY.Value);
        _spScaZ.ValueCommitted += (_, _) => _vm.ApplyScaleZ(_spScaZ.Value);

        _btnAddBehaviour.Click += OnAddBehaviourClick;

        _vm.RefreshRequested              += OnRefreshRequested;
        _vm.TransformOnlyRefreshRequested += PopulateTransform;

        _vm.Attach();

        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _vm.Detach();
        base.Dispose(disposing);
    }

    // ── Actualización UI ──────────────────────────────────────────────────────

    private void OnRefreshRequested()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(OnRefreshRequested); return; }

        bool has = _vm.HasSelection;

        _lblName.Text = has ? _vm.ObjectName  : "—";
        _lblId.Text   = has ? $"ID: {_vm.ObjectIdShort}" : string.Empty;

        bool enabled = has && _vm.ContentEnabled;
        _chkActive.Enabled = enabled;
        if (has && _vm.Selected is { } sel)
            _chkActive.Checked = sel.Active;

        _btnAddBehaviour.Enabled = enabled;

        PopulateTransform();
        BuildBehaviourCards();
    }

    private void PopulateTransform()
    {
        if (!IsHandleCreated) return;
        if (InvokeRequired) { Invoke(PopulateTransform); return; }

        EditorGameObject? sel = _vm.Selected;
        bool enabled = sel is not null && _vm.ContentEnabled;

        void SetStepper(AxisStepper sp, double value)
        {
            sp.Enabled = enabled;
            sp.Value   = value;
        }

        SetStepper(_spPosX, sel?.Position.X ?? 0);
        SetStepper(_spPosY, sel?.Position.Y ?? 0);
        SetStepper(_spPosZ, sel?.Position.Z ?? 0);
        SetStepper(_spRotX, sel?.Rotation.X ?? 0);
        SetStepper(_spRotY, sel?.Rotation.Y ?? 0);
        SetStepper(_spRotZ, sel?.Rotation.Z ?? 0);
        SetStepper(_spScaX, sel?.Scale.X ?? 1);
        SetStepper(_spScaY, sel?.Scale.Y ?? 1);
        SetStepper(_spScaZ, sel?.Scale.Z ?? 1);
    }

    // ── Tarjetas de Behaviour ─────────────────────────────────────────────────

    private void BuildBehaviourCards()
    {
        _behaviourArea.SuspendLayout();
        _behaviourArea.Controls.Clear();

        EditorGameObject? sel = _vm.Selected;
        if (sel is null || !_vm.ContentEnabled)
        {
            _behaviourArea.ResumeLayout(false);
            return;
        }

        // WinForms Dock=Top apila de abajo hacia arriba → añadir en orden inverso
        for (int i = sel.Behaviours.Count - 1; i >= 0; i--)
        {
            EditorBehaviour behaviour = sel.Behaviours[i];
            Panel card = BuildBehaviourCard(sel, behaviour);
            card.Dock = DockStyle.Top;
            _behaviourArea.Controls.Add(card);
        }

        _behaviourArea.ResumeLayout(true);
    }

    private Panel BuildBehaviourCard(EditorGameObject owner, EditorBehaviour behaviour)
    {
        string shortName = GetShortTypeName(behaviour.TypeName);

        // Cabecera de la tarjeta
        Panel header = new()
        {
            Height    = 28,
            BackColor = EditorColors.PanelBackgroundAlt,
        };

        Label title = new()
        {
            Text      = shortName,
            ForeColor = EditorColors.TextPrimary,
            Font      = EditorFonts.PrimaryBold,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(6, 0, 0, 0),
            Dock      = DockStyle.Fill,
        };

        Button btnDelete = new()
        {
            Text      = "✕",
            Width     = 24,
            Dock      = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextMuted,
            Font      = EditorFonts.Small,
            TabStop   = false,
        };
        btnDelete.FlatAppearance.BorderSize = 0;
        btnDelete.Click += (_, _) =>
        {
            EditorContext.Instance.Commands.Execute(new RemoveBehaviourCommand(owner, behaviour));
            EditorContext.Instance.EventBus.Publish(new GameObjectPropertyChangedEvent(owner));
            BuildBehaviourCards();
        };

        header.Controls.Add(title);
        header.Controls.Add(btnDelete);

        // Cuerpo del editor
        BehaviourEditor? editor = BehaviourEditorRegistry.GetEditor(behaviour.TypeName);
        Control body;
        if (editor is not null)
        {
            BehaviourEditorRegistry.PrepareEditor(editor);
            try { body = editor.BuildInspector(behaviour, owner); }
            catch { body = MakeFallbackLabel($"Error building inspector for {shortName}"); }
        }
        else
        {
            body = MakeFallbackLabel($"No editor for {shortName}");
        }
        body.Dock = DockStyle.Top;

        Panel card = new()
        {
            AutoSize     = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor    = EditorColors.PanelBackground,
        };
        card.Controls.Add(body);
        card.Controls.Add(header);

        return card;
    }

    private void OnAddBehaviourClick(object? sender, EventArgs e)
    {
        EditorGameObject? sel = _vm.Selected;
        if (sel is null) return;

        string? typeName = AddBehaviourForm.Show(FindForm(), _vm.Registry);
        if (typeName is null) return;

        EditorBehaviour behaviour = new() { TypeName = typeName };
        EditorContext.Instance.Commands.Execute(new AddBehaviourCommand(sel, behaviour));
        EditorContext.Instance.EventBus.Publish(new GameObjectPropertyChangedEvent(sel));
        BuildBehaviourCards();
    }

    private static Label MakeFallbackLabel(string message) => new()
    {
        Text      = message,
        ForeColor = EditorColors.TextMuted,
        Font      = EditorFonts.Small,
        Height    = 22,
        Padding   = new Padding(6, 0, 0, 0),
    };

    private static string GetShortTypeName(string typeName)
    {
        int comma = typeName.IndexOf(',');
        string noAssembly = comma > 0 ? typeName[..comma].Trim() : typeName;
        int dot = noAssembly.LastIndexOf('.');
        return dot >= 0 ? noAssembly[(dot + 1)..] : noAssembly;
    }

    // ── Helpers de layout ─────────────────────────────────────────────────────

    private TableLayoutPanel BuildTransformGrid()
    {
        TableLayoutPanel grid = new()
        {
            ColumnCount = 4,
            RowCount    = 3,
            Margin      = Padding.Empty,
            Padding     = new Padding(2),
        };

        // 4 columnas: etiqueta fija (56px) + 3 steppers que dividen el resto
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

        for (int r = 0; r < 3; r++)
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));

        // Etiquetas de fila
        grid.Controls.Add(MakeRowLabel("Position"), 0, 0);
        grid.Controls.Add(MakeRowLabel("Rotation"), 0, 1);
        grid.Controls.Add(MakeRowLabel("Scale"),    0, 2);

        // Steppers
        grid.Controls.Add(_spPosX, 1, 0); grid.Controls.Add(_spPosY, 2, 0); grid.Controls.Add(_spPosZ, 3, 0);
        grid.Controls.Add(_spRotX, 1, 1); grid.Controls.Add(_spRotY, 2, 1); grid.Controls.Add(_spRotZ, 3, 1);
        grid.Controls.Add(_spScaX, 1, 2); grid.Controls.Add(_spScaY, 2, 2); grid.Controls.Add(_spScaZ, 3, 2);

        return grid;
    }

    private static Label MakeRowLabel(string text) => new()
    {
        Text      = text,
        ForeColor = EditorColors.TextSecondary,
        Font      = EditorFonts.Small,
        TextAlign = ContentAlignment.MiddleLeft,
        Dock      = DockStyle.Fill,
        Padding   = new Padding(2, 0, 0, 0),
    };

    private static AxisStepper MakeStepper(string axis) => new()
    {
        Axis    = axis,
        Value   = 0,
        Step    = 0.1,
        Enabled = false,
        Margin  = new Padding(1),
        Dock    = DockStyle.Fill,
    };

    private static void ApplyTabStyle(TabControl tabs)
    {
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.DrawItem += (s, e) =>
        {
            TabControl tc = (TabControl)s!;
            Rectangle  r  = tc.GetTabRect(e.Index);
            bool       sel = e.Index == tc.SelectedIndex;

            using SolidBrush bg = new(sel ? EditorColors.PanelBackground : EditorColors.PanelBackgroundAlt);
            e.Graphics.FillRectangle(bg, r);

            using SolidBrush fg = new(sel ? EditorColors.TextPrimary : EditorColors.TextSecondary);
            StringFormat sf = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(tc.TabPages[e.Index].Text, EditorFonts.Primary, fg, r, sf);
        };
    }
}
