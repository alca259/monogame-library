namespace MonoGame.Editor.WinForms.Panels;

/// <summary>
/// Muestra y edita las propiedades del <see cref="EditorGameObject"/> actualmente seleccionado.
/// La sección Transform es siempre visible; se generan secciones adicionales por cada
/// <see cref="EditorBehaviour"/> adjunto, usando reflexión sobre <see cref="EditorPropertyAttribute"/>.
/// Todas las modificaciones se enrutan a través de <see cref="CommandStack"/> para soporte de deshacer/rehacer.
/// </summary>
public sealed class InspectorPanel : UserControl
{
    #region Constants

    private const int LabelWidth    = 72;
    private const int RowHeight     = 26;
    private const int LabelHeight   = 16;
    private const int StackedRow    = LabelHeight + RowHeight;
    private const int SectionGap    = 6;
    private const int SidePadding   = 6;
    private const int NumericWidth  = 68;
    private const int HeaderHeight  = 56;

    #endregion

    #region Fields

    private EditorContext?      _context;
    private GameObjectRegistry? _registry;
    private PrefabManager?      _prefabManager;
    private EditorPreferences?  _preferences;
    private EditorGameObject?   _currentObject;
    private bool                _suppressUpdate;

    private readonly Panel _scrollPanel;

    private Action<UndoPerformedEvent>? _onUndo;
    private Action<RedoPerformedEvent>? _onRedo;
    private Action<GameObjectTransformChangedEvent>? _onTransformChanged;

    private NumericUpDown? _positionXInput;
    private NumericUpDown? _positionYInput;
    private NumericUpDown? _positionZInput;
    private NumericUpDown? _rotationInput;
    private NumericUpDown? _scaleXInput;
    private NumericUpDown? _scaleYInput;

    private readonly ToolTip _toolTip;

    #endregion

    #region Constructor

    /// <summary>Crea el panel. Llama a <see cref="Initialize"/> para conectar con el contexto del editor.</summary>
    public InspectorPanel()
    {
        _toolTip = new ToolTip { AutoPopDelay = 4000, InitialDelay = 500, ReshowDelay = 200 };
        _scrollPanel = new Panel
        {
            Dock       = DockStyle.Fill,
            AutoScroll = true,
        };
        Controls.Add(_scrollPanel);
        _scrollPanel.ClientSizeChanged += OnScrollPanelResized;
    }

    #endregion

    #region Initialization

    /// <summary>Conecta este panel con el contexto del editor y el registro de comportamientos.</summary>
    public void Initialize(EditorContext context, GameObjectRegistry? registry = null, PrefabManager? prefabManager = null, EditorPreferences? preferences = null)
    {
        _context       = context;
        _registry      = registry;
        _prefabManager = prefabManager;
        _preferences   = preferences;

        _onUndo = _ => RebuildSafe();
        _onRedo = _ => RebuildSafe();
        _onTransformChanged = OnGameObjectTransformChanged;

        _context.EventBus.Subscribe<GameObjectSelectedEvent>(OnGameObjectSelected);
        _context.EventBus.Subscribe<UndoPerformedEvent>(_onUndo);
        _context.EventBus.Subscribe<RedoPerformedEvent>(_onRedo);
        _context.EventBus.Subscribe<GameObjectTransformChangedEvent>(_onTransformChanged);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _context is not null)
        {
            _context.EventBus.Unsubscribe<GameObjectSelectedEvent>(OnGameObjectSelected);
            if (_onUndo is not null) _context.EventBus.Unsubscribe<UndoPerformedEvent>(_onUndo);
            if (_onRedo is not null) _context.EventBus.Unsubscribe<RedoPerformedEvent>(_onRedo);
            if (_onTransformChanged is not null) _context.EventBus.Unsubscribe<GameObjectTransformChangedEvent>(_onTransformChanged);
        }
        if (disposing) _toolTip.Dispose();
        base.Dispose(disposing);
    }

    #endregion

    #region Event handlers

    private void OnGameObjectSelected(GameObjectSelectedEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(() => OnGameObjectSelected(evt)); return; }
        _currentObject = evt.GameObject;
        RebuildContent();
    }

    private void RebuildSafe()
    {
        if (IsDisposed || Disposing || !IsHandleCreated)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(() =>
            {
                if (!IsDisposed && !Disposing && IsHandleCreated)
                    RebuildContent();
            });
            return;
        }

        RebuildContent();
    }

    private void OnGameObjectTransformChanged(GameObjectTransformChangedEvent evt)
    {
        if (_currentObject is null || !ReferenceEquals(_currentObject, evt.GameObject))
            return;

        UpdateTransformInputsFromCurrentObject();
    }

    private void OnScrollPanelResized(object? sender, EventArgs e)
    {
        int w = ContentWidth();
        foreach (Control c in _scrollPanel.Controls)
            c.Width = w;
    }

    #endregion

    #region Content building

    private void RebuildContent()
    {
        _suppressUpdate = true;
        _scrollPanel.SuspendLayout();
        _scrollPanel.Controls.Clear();
        _positionXInput = null;
        _positionYInput = null;
        _positionZInput = null;
        _rotationInput = null;
        _scaleXInput = null;
        _scaleYInput = null;

        if (_currentObject is null)
        {
            Label placeholder = new Label
            {
                Text      = "Nothing selected",
                Dock      = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                ForeColor = System.Drawing.SystemColors.GrayText,
            };
            _scrollPanel.Controls.Add(placeholder);
            _scrollPanel.ResumeLayout();
            _suppressUpdate = false;
            return;
        }

        IReadOnlyList<EditorGameObject> multi = _context?.MultiSelection ?? [];
        if (multi.Count > 1)
        {
            BuildMultiSelectionContent(multi);
            _scrollPanel.ResumeLayout();
            _suppressUpdate = false;
            return;
        }

        int y     = SidePadding;
        int width = ContentWidth();

        // Encabezado de entidad
        Control entityHeader = BuildEntityHeader(_currentObject);
        entityHeader.Location = new System.Drawing.Point(SidePadding, y);
        entityHeader.Width    = width;
        _scrollPanel.Controls.Add(entityHeader);
        y += entityHeader.Height + SectionGap;

        // Encabezado de prefab (solo cuando el objeto es una instancia de prefab)
        if (_currentObject.PrefabPath is not null && _prefabManager is not null)
        {
            Control prefabHeader = BuildPrefabHeader(_currentObject);
            prefabHeader.Location = new System.Drawing.Point(SidePadding, y);
            prefabHeader.Width    = width;
            _scrollPanel.Controls.Add(prefabHeader);
            y += prefabHeader.Height + SectionGap;
        }

        // Sección de transformación
        Control transformSection = BuildTransformSection(_currentObject);
        transformSection.Location = new System.Drawing.Point(SidePadding, y);
        transformSection.Width    = width;
        _scrollPanel.Controls.Add(transformSection);
        y += transformSection.Height + SectionGap;

        // Una sección por comportamiento
        for (int i = 0; i < _currentObject.Behaviours.Count; i++)
        {
            EditorBehaviour b = _currentObject.Behaviours[i];
            Control section   = BuildBehaviourSection(b, _currentObject);
            section.Location  = new System.Drawing.Point(SidePadding, y);
            section.Width     = width;
            _scrollPanel.Controls.Add(section);
            y += section.Height + SectionGap;
        }

        // Botón de agregar comportamiento
        Panel addPanel = new Panel { Height = 36, Location = new System.Drawing.Point(SidePadding, y) };
        Button addBtn = new Button
        {
            Text      = "+ Add Behaviour",
            Height    = 28,
            Dock      = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Padding   = new System.Windows.Forms.Padding(4, 2, 4, 2),
        };
        addBtn.FlatAppearance.BorderColor = System.Drawing.SystemColors.ControlDark;
        addBtn.Click += OnAddBehaviourClick;
        addPanel.Controls.Add(addBtn);
        addPanel.Width = width;
        _scrollPanel.Controls.Add(addPanel);

        _scrollPanel.ResumeLayout();
        _suppressUpdate = false;
    }

    private int ContentWidth() =>
        Math.Max(0, _scrollPanel.ClientSize.Width - SidePadding * 2 - SystemInformation.VerticalScrollBarWidth);

    private void BuildMultiSelectionContent(IReadOnlyList<EditorGameObject> objects)
    {
        int y     = SidePadding;
        int width = ContentWidth();

        // Encabezado
        Panel header = new Panel
        {
            Height    = 32,
            BackColor = System.Drawing.SystemColors.ControlDarkDark,
            Location  = new System.Drawing.Point(SidePadding, y),
            Width     = width,
        };
        Label countLabel = new Label
        {
            Dock      = DockStyle.Fill,
            Text      = $"{objects.Count} objects selected",
            Font      = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold),
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
        };
        header.Controls.Add(countLabel);
        _scrollPanel.Controls.Add(header);
        y += header.Height + SectionGap;

        // Sección de transformación delta
        GroupBox grp = new GroupBox
        {
            Text     = "Move Selection (Δ)",
            Height   = 24 + RowHeight * 2 + 12,
            Location = new System.Drawing.Point(SidePadding, y),
            Width    = width,
            Padding  = new System.Windows.Forms.Padding(4),
        };

        TableLayoutPanel table = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 2,
            Padding     = new System.Windows.Forms.Padding(0),
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelWidth));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, RowHeight));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, RowHeight));

        // Fila ΔX
        table.Controls.Add(MakeLabel("ΔX"), 0, 0);
        Panel deltaXPanel = new Panel { Height = RowHeight };
        NumericUpDown deltaXInput = CreateNumericUpDown(0m, -100_000m, 100_000m, 1);
        deltaXInput.Dock = DockStyle.Fill;
        _toolTip.SetToolTip(deltaXInput, "Offset all selected objects by this amount on the X axis");
        deltaXPanel.Controls.Add(deltaXInput);
        table.Controls.Add(deltaXPanel, 1, 0);

        // Fila ΔY
        table.Controls.Add(MakeLabel("ΔY"), 0, 1);
        Panel deltaYPanel = new Panel { Height = RowHeight };
        NumericUpDown deltaYInput = CreateNumericUpDown(0m, -100_000m, 100_000m, 1);
        deltaYInput.Dock = DockStyle.Fill;
        _toolTip.SetToolTip(deltaYInput, "Offset all selected objects by this amount on the Y axis");
        deltaYPanel.Controls.Add(deltaYInput);
        table.Controls.Add(deltaYPanel, 1, 1);

        grp.Controls.Add(table);
        _scrollPanel.Controls.Add(grp);
        y += grp.Height + SectionGap;

        // Botón de aplicar
        Panel applyPanel = new Panel { Height = 36, Location = new System.Drawing.Point(SidePadding, y), Width = width };
        Button applyBtn = new Button
        {
            Text      = "Apply Delta",
            Height    = 28,
            Dock      = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Padding   = new System.Windows.Forms.Padding(4, 2, 4, 2),
        };
        applyBtn.FlatAppearance.BorderColor = System.Drawing.SystemColors.ControlDark;
        applyBtn.Click += (_, _) =>
        {
            float dx = (float)deltaXInput.Value;
            float dy = (float)deltaYInput.Value;
            if (dx == 0f && dy == 0f) return;
            EditorGameObject[] snapshot = [.. objects];
            _context!.Commands.Execute(new BatchMoveCommand(snapshot, new EditorVector2(dx, dy)));
            deltaXInput.Value = 0m;
            deltaYInput.Value = 0m;
        };
        applyPanel.Controls.Add(applyBtn);
        _scrollPanel.Controls.Add(applyPanel);
    }

    #endregion

    #region Entity header

    private Control BuildEntityHeader(EditorGameObject obj)
    {
        Panel panel = new Panel
        {
            Height    = HeaderHeight,
            BackColor = System.Drawing.SystemColors.ControlDarkDark,
            Padding   = new System.Windows.Forms.Padding(4, 2, 4, 2),
        };

        // Etiqueta de Id (Dock=Bottom)
        Label idLabel = new Label
        {
            Dock      = DockStyle.Bottom,
            Height    = 16,
            Text      = obj.Id.ToString()[..8],
            Font      = new System.Drawing.Font("Segoe UI", 7f),
            ForeColor = System.Drawing.SystemColors.GrayText,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
        };

        // Casilla de verificación activo (Dock=Left)
        CheckBox activeChk = new CheckBox
        {
            Dock    = DockStyle.Left,
            Width   = 18,
            Checked = obj.Active,
        };
        activeChk.CheckedChanged += (_, _) =>
        {
            if (_suppressUpdate) return;
            _context!.Commands.Execute(new SetPropertyCommand<bool>(
                "Set Active", obj.Active, activeChk.Checked, v => obj.Active = v));
        };

        // Combobox de etiquetas (Dock=Right)
        ComboBox tagsCombo = new ComboBox
        {
            Dock          = DockStyle.Right,
            Width         = 90,
            DropDownStyle = ComboBoxStyle.DropDown,
        };
        tagsCombo.Text = "Add tag...";
        for (int i = 0; i < obj.Tags.Count; i++)
            tagsCombo.Items.Add(obj.Tags[i]);
        tagsCombo.KeyDown += (_, ev) =>
        {
            if (ev.KeyCode != Keys.Enter) return;
            string tag = tagsCombo.Text.Trim();
            if (string.IsNullOrEmpty(tag) || obj.Tags.Contains(tag)) return;
            List<string> newTags = [.. obj.Tags, tag];
            _context!.Commands.Execute(new SetTagsCommand(obj, newTags));
            tagsCombo.Items.Clear();
            for (int i = 0; i < obj.Tags.Count; i++) tagsCombo.Items.Add(obj.Tags[i]);
            tagsCombo.Text = string.Empty;
            ev.Handled = true;
            ev.SuppressKeyPress = true;
        };

        // Cuadro de texto de nombre de entidad (Dock=Fill)
        TextBox nameBox = new TextBox
        {
            Dock      = DockStyle.Fill,
            Text      = obj.Name,
            Font      = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold),
            BackColor = System.Drawing.SystemColors.ControlDarkDark,
            ForeColor = System.Drawing.SystemColors.ControlText,
            BorderStyle = BorderStyle.None,
        };
        nameBox.Leave += (_, _) =>
        {
            string newName = nameBox.Text.Trim();
            if (string.IsNullOrEmpty(newName) || newName == obj.Name) return;
            _context!.Commands.Execute(new RenameEntityCommand(obj, newName));
        };

        panel.Controls.Add(idLabel);
        panel.Controls.Add(tagsCombo);
        panel.Controls.Add(nameBox);
        panel.Controls.Add(activeChk);
        return panel;
    }

    #endregion

    #region Transform section

    private Control BuildTransformSection(EditorGameObject obj)
    {
        GroupBox grp = new GroupBox
        {
            Text    = "Transform",
            Height  = 24 + RowHeight * 4 + 12,
            Padding = new System.Windows.Forms.Padding(4),
        };

        TableLayoutPanel table = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 4,
            Padding     = new System.Windows.Forms.Padding(0),
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelWidth));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, RowHeight));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, RowHeight));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, RowHeight));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, RowHeight));

        // Posición
        table.Controls.Add(MakeLabel("Local Position"), 0, 0);
        table.Controls.Add(BuildTransformVec2Editor(
            obj.LocalPosition.X,
            obj.LocalPosition.Y,
            (nx, ny) =>
            {
                _positionXInput = nx;
                _positionYInput = ny;
            },
            (x, y) =>
            {
                if (_suppressUpdate) return;
                _context!.Commands.Execute(new MoveEntityCommand(obj, obj.Position, obj.Parent is null
                    ? new EditorVector2(x, y)
                    : new EditorVector2(obj.Parent.Position.X + x, obj.Parent.Position.Y + y)));
            }), 1, 0);

        // Rotación
        table.Controls.Add(MakeLabel("Local Rotation"), 0, 1);
        table.Controls.Add(BuildTransformFloatEditor(
            obj.LocalRotation,
            -360f,
            360f,
            input => _rotationInput = input,
            v =>
            {
                if (_suppressUpdate) return;
                _context!.Commands.Execute(new RotateEntityCommand(obj, obj.Rotation, obj.Parent is null ? v : obj.Parent.Rotation + v));
            }), 1, 1);

        // Escala
        table.Controls.Add(MakeLabel("Local Scale"), 0, 2);
        table.Controls.Add(BuildTransformVec2Editor(
            obj.LocalScale.X,
            obj.LocalScale.Y,
            (nx, ny) =>
            {
                _scaleXInput = nx;
                _scaleYInput = ny;
            },
            (x, y) =>
            {
                if (_suppressUpdate) return;
                _context!.Commands.Execute(new ScaleEntityCommand(obj, obj.Scale, obj.Parent is null
                    ? new EditorVector2(x, y)
                    : new EditorVector2(obj.Parent.Scale.X * x, obj.Parent.Scale.Y * y)));
            }), 1, 2);

        // Profundidad Z (ordenamiento por parallax 2.5D)
        table.Controls.Add(MakeLabel("Depth Z"), 0, 3);
        table.Controls.Add(BuildTransformFloatEditor(
            obj.PositionZ,
            -10_000f,
            10_000f,
            input => _positionZInput = input,
            v =>
            {
                if (_suppressUpdate) return;
                _context!.Commands.Execute(new MoveEntityZCommand(obj, obj.PositionZ, v));
            }), 1, 3);

        grp.Controls.Add(table);
        return grp;
    }

    private Panel BuildTransformFloatEditor(
        float value,
        float min,
        float max,
        Action<NumericUpDown> captureInput,
        Action<float> onChange)
    {
        Panel panel = new Panel { Height = RowHeight };
        NumericUpDown num = CreateNumericUpDown((decimal)value,
            (decimal)Math.Max(min, (float)decimal.MinValue),
            (decimal)Math.Min(max, (float)decimal.MaxValue), 3);
        num.Dock = DockStyle.Fill;
        num.ValueChanged += (_, _) => onChange((float)num.Value);
        panel.Controls.Add(num);
        captureInput(num);
        return panel;
    }

    private Panel BuildTransformVec2Editor(
        float x,
        float y,
        Action<NumericUpDown, NumericUpDown> captureInputs,
        Action<float, float> onChange)
    {
        Panel panel = new Panel { Height = RowHeight };

        Label lx = new Label { Text = "X", Width = 14, Dock = DockStyle.Left, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
        NumericUpDown nx = CreateNumericUpDown((decimal)x, -1_000_000m, 1_000_000m, 3);
        nx.Width = NumericWidth;
        nx.Dock  = DockStyle.Left;

        Label ly = new Label { Text = "Y", Width = 14, Dock = DockStyle.Left, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
        NumericUpDown ny = CreateNumericUpDown((decimal)y, -1_000_000m, 1_000_000m, 3);
        ny.Dock = DockStyle.Fill;

        panel.Controls.Add(ny);
        panel.Controls.Add(ly);
        panel.Controls.Add(nx);
        panel.Controls.Add(lx);

        nx.ValueChanged += (_, _) => onChange((float)nx.Value, (float)ny.Value);
        ny.ValueChanged += (_, _) => onChange((float)nx.Value, (float)ny.Value);

        captureInputs(nx, ny);
        return panel;
    }

    private void UpdateTransformInputsFromCurrentObject()
    {
        if (_currentObject is null)
            return;

        if (_positionXInput is null || _positionYInput is null || _rotationInput is null || _scaleXInput is null || _scaleYInput is null || _positionZInput is null)
        {
            RebuildSafe();
            return;
        }

        bool previousSuppress = _suppressUpdate;
        _suppressUpdate = true;
        try
        {
            SetNumericValue(_positionXInput, _currentObject.LocalPosition.X);
            SetNumericValue(_positionYInput, _currentObject.LocalPosition.Y);
            SetNumericValue(_rotationInput, _currentObject.LocalRotation);
            SetNumericValue(_scaleXInput, _currentObject.LocalScale.X);
            SetNumericValue(_scaleYInput, _currentObject.LocalScale.Y);
            SetNumericValue(_positionZInput, _currentObject.PositionZ);
        }
        finally
        {
            _suppressUpdate = previousSuppress;
        }
    }

    private static void SetNumericValue(NumericUpDown input, float value)
    {
        decimal next = Math.Clamp((decimal)value, input.Minimum, input.Maximum);
        if (input.Value != next)
            input.Value = next;
    }

    #endregion

    #region Behaviour section

    private Control BuildBehaviourSection(EditorBehaviour behaviour, EditorGameObject owner)
    {
        string shortName = ExtractShortName(behaviour.TypeName);
        bool collapsed   = _preferences?.BehaviourSectionCollapsed.GetValueOrDefault(shortName, false) ?? false;

        // Panel externo que apila encabezado + cuerpo
        Panel outerPanel = new Panel { Padding = new System.Windows.Forms.Padding(0) };

        // --- Encabezado ---
        Panel header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 28,
            BackColor = System.Drawing.SystemColors.ControlDark,
            Padding   = new System.Windows.Forms.Padding(2),
        };

        Label chevron = new Label
        {
            Text      = collapsed ? "▶" : "▼",
            Dock      = DockStyle.Left,
            Width     = 16,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Cursor    = Cursors.Hand,
        };

        Label nameLabel = new Label
        {
            Text      = shortName,
            Dock      = DockStyle.Left,
            AutoSize  = false,
            Width     = 152,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
        };

        CheckBox enabledChk = new CheckBox
        {
            Text    = "On",
            Checked = behaviour.Enabled,
            Dock    = DockStyle.Left,
            Width   = 44,
        };
        enabledChk.CheckedChanged += (_, _) =>
        {
            if (_suppressUpdate) return;
            bool prev = behaviour.Enabled;
            bool next = enabledChk.Checked;
            _context!.Commands.Execute(new SetPropertyCommand<bool>(
                "Set Behaviour Enabled", prev, next, v => behaviour.Enabled = v));
        };

        Button removeBtn = new Button
        {
            Text  = "×",
            Dock  = DockStyle.Right,
            Width = 24,
        };
        EditorBehaviour capturedBehaviour = behaviour;
        removeBtn.Click += (_, _) =>
        {
            if (_currentObject is null) return;
            _context!.Commands.Execute(new RemoveBehaviourCommand(_currentObject, capturedBehaviour));
            RebuildContent();
        };

        header.Controls.Add(removeBtn);
        header.Controls.Add(enabledChk);
        header.Controls.Add(nameLabel);
        header.Controls.Add(chevron);

        // --- Cuerpo (propiedades) ---
        List<(string label, Control ctrl)> rows = BuildPropertyRows(behaviour, owner);
        int bodyHeight = rows.Count * StackedRow + 8;

        Panel body = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = bodyHeight,
            Padding   = new System.Windows.Forms.Padding(4, 2, 4, 2),
            Visible   = !collapsed,
        };

        for (int i = 0; i < rows.Count; i++)
        {
            int rowY = i * StackedRow + 2;

            Label lbl = MakeStackedLabel(rows[i].label);
            lbl.Location = new System.Drawing.Point(4, rowY);
            lbl.Width    = body.Width - 8;
            body.Controls.Add(lbl);

            Control ctrl = rows[i].ctrl;
            ctrl.Location = new System.Drawing.Point(4, rowY + LabelHeight);
            ctrl.Width    = body.Width - 8;
            body.Controls.Add(ctrl);
        }

        // Alternar colapso
        string capturedName = shortName;
        chevron.Click += (_, _) => ToggleSectionCollapse(capturedName, chevron, body, outerPanel, bodyHeight);
        header.Click  += (_, _) => ToggleSectionCollapse(capturedName, chevron, body, outerPanel, bodyHeight);

        // Agregar cuerpo primero, luego encabezado (DockStyle.Top: el último agregado queda en la parte superior)
        outerPanel.Controls.Add(body);
        outerPanel.Controls.Add(header);
        outerPanel.Height = collapsed ? 28 : 28 + bodyHeight;

        return outerPanel;
    }

    private void ToggleSectionCollapse(string sectionName, Label chevron, Panel body, Panel outer, int bodyHeight)
    {
        bool nowCollapsed = body.Visible;
        body.Visible  = !nowCollapsed;
        chevron.Text  = nowCollapsed ? "▶" : "▼";
        outer.Height  = nowCollapsed ? 28 : 28 + bodyHeight;
        if (_preferences is not null)
        {
            _preferences.BehaviourSectionCollapsed[sectionName] = nowCollapsed;
            _preferences.Save();
        }
    }

    private List<(string, Control)> BuildPropertyRows(EditorBehaviour behaviour, EditorGameObject owner)
    {
        List<(string, Control)> rows = [];

        if (_registry is null || string.IsNullOrEmpty(behaviour.TypeName)) return rows;
        if (!_registry.RegisteredTypes.TryGetValue(behaviour.TypeName, out Type? type)) return rows;

        PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Comprueba si ALGUNA propiedad tiene el atributo explícito.
        bool hasAnyAttribute = false;
        for (int i = 0; i < props.Length; i++)
        {
            if (props[i].GetCustomAttribute<EditorPropertyAttribute>() is not null)
            {
                hasAnyAttribute = true;
                break;
            }
        }

        for (int i = 0; i < props.Length; i++)
        {
            PropertyInfo prop = props[i];
            EditorPropertyAttribute? attr = prop.GetCustomAttribute<EditorPropertyAttribute>();

            // Si este tipo no tiene [EditorProperty] en absoluto (p. ej., tipos de la librería Kernel),
            // muestra como alternativa todas las propiedades públicas de lectura/escritura de los tipos admitidos.
        bool include = attr is not null
                || (!hasAnyAttribute && prop.CanRead && prop.CanWrite
                    && IsSupportedFallbackType(prop.PropertyType));

            if (!include) continue;

            // Oculta los miembros internos locales de tiempo de ejecución y los alias de mundo en la vista de respaldo de TransformBehaviour.
            if (string.Equals(type.Name, "TransformBehaviour", StringComparison.Ordinal)
                && (prop.Name.StartsWith("Local", StringComparison.Ordinal)
                    || string.Equals(prop.Name, "LocalToWorldMatrix", StringComparison.Ordinal)
                    || string.Equals(prop.Name, "WorldToLocalMatrix", StringComparison.Ordinal)
                    || string.Equals(prop.Name, "ParentTransform", StringComparison.Ordinal)
                    || string.Equals(prop.Name, "Root", StringComparison.Ordinal)
                    || string.Equals(prop.Name, "ChildCount", StringComparison.Ordinal)
                    || string.Equals(prop.Name, "Enabled", StringComparison.Ordinal)))
            {
                continue;
            }

            // Enabled siempre se muestra en el encabezado de sección como "On"; omitirlo para todos los comportamientos.
            if (string.Equals(prop.Name, "Enabled", StringComparison.Ordinal))
                continue;

            string label = attr?.Label ?? prop.Name;
            Control ctrl = CreateControlForProperty(prop, attr, behaviour, owner);
            if (attr?.Tooltip is { Length: > 0 } tip)
                _toolTip.SetToolTip(ctrl, tip);
            rows.Add((label, ctrl));
        }
        return rows;
    }

    private static bool IsSupportedFallbackType(Type t) =>
        t == typeof(bool)
        || t == typeof(int)
        || t == typeof(float)
        || t == typeof(string)
        || t == typeof(Vector2)
        || t == typeof(Vector3)
        || t == typeof(Microsoft.Xna.Framework.Color)
        || t == typeof(System.Drawing.Color);

    private Control CreateControlForProperty(
        PropertyInfo prop,
        EditorPropertyAttribute? attr,
        EditorBehaviour behaviour,
        EditorGameObject _)
    {
        Type pType = prop.PropertyType;

        if (pType == typeof(bool))
        {
            bool current = behaviour.Properties.TryGetValue(prop.Name, out JsonElement el)
                ? el.GetBoolean() : false;
            CheckBox chk = new CheckBox { Checked = current };
            chk.CheckedChanged += (_, _) =>
            {
                bool prev = behaviour.Properties.TryGetValue(prop.Name, out JsonElement prevEl)
                    ? prevEl.GetBoolean() : !chk.Checked;
                JsonElement newEl = JsonDocument.Parse(chk.Checked ? "true" : "false").RootElement;
                _context!.Commands.Execute(new SetPropertyCommand<JsonElement>(
                    $"Set {prop.Name}", prevEl, newEl,
                    v => behaviour.Properties[prop.Name] = v));
            };
            return chk;
        }

        if (pType == typeof(string))
        {
            string current = behaviour.Properties.TryGetValue(prop.Name, out JsonElement el)
                ? el.GetString() ?? string.Empty : string.Empty;
            TextBox tb = new TextBox { Text = current };
            tb.Leave += (_, _) =>
            {
                string prev = behaviour.Properties.TryGetValue(prop.Name, out JsonElement prevEl)
                    ? prevEl.GetString() ?? string.Empty : string.Empty;
                if (prev == tb.Text) return;
                JsonElement newEl = JsonDocument.Parse($"\"{tb.Text}\"").RootElement;
                _context!.Commands.Execute(new SetPropertyCommand<JsonElement>(
                    $"Set {prop.Name}", prevEl, newEl,
                    v => behaviour.Properties[prop.Name] = v));
            };
            return tb;
        }

        if (pType == typeof(float) || pType == typeof(int))
        {
            float current = behaviour.Properties.TryGetValue(prop.Name, out JsonElement el)
                ? el.GetSingle() : 0f;
            float min = (attr?.Min ?? 0f) == float.MinValue ? -1_000_000f : (attr?.Min ?? -1_000_000f);
            float max = (attr?.Max ?? 0f) == float.MaxValue ?  1_000_000f : (attr?.Max ??  1_000_000f);
            return MakeFloatControl(current, min, max, v =>
            {
                if (_suppressUpdate) return;
                float prev = behaviour.Properties.TryGetValue(prop.Name, out JsonElement prevEl)
                    ? prevEl.GetSingle() : 0f;
                if (Math.Abs(prev - v) < 0.0001f) return;
                JsonElement newEl = JsonDocument.Parse(v.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).RootElement;
                _context!.Commands.Execute(new SetPropertyCommand<JsonElement>(
                    $"Set {prop.Name}", prevEl, newEl,
                    ev => behaviour.Properties[prop.Name] = ev));
            }, attr);
        }

        if (pType.IsEnum)
        {
            bool isFlags = pType.GetCustomAttribute<FlagsAttribute>() is not null;

            if (isFlags)
            {
                CheckedListBox clb = new CheckedListBox { CheckOnClick = true, Height = RowHeight * 3 };
                string[] names = Enum.GetNames(pType);
                for (int i = 0; i < names.Length; i++) clb.Items.Add(names[i]);

                if (behaviour.Properties.TryGetValue(prop.Name, out JsonElement flagEl))
                {
                    string? flagStr = flagEl.GetString() ?? string.Empty;
                    for (int i = 0; i < clb.Items.Count; i++)
                        clb.SetItemChecked(i, flagStr.Contains(clb.Items[i]!.ToString()!, StringComparison.Ordinal));
                }

                clb.ItemCheck += (_, _) =>
                {
                    List<string> checked_ = [];
                    for (int i = 0; i < clb.Items.Count; i++)
                        if (clb.GetItemChecked(i)) checked_.Add(clb.Items[i]!.ToString()!);
                    string combined = string.Join(", ", checked_);
                    JsonElement prev = behaviour.Properties.TryGetValue(prop.Name, out JsonElement prevEl) ? prevEl : default;
                    JsonElement newEl = JsonDocument.Parse($"\"{combined}\"").RootElement;
                    _context!.Commands.Execute(new SetPropertyCommand<JsonElement>(
                        $"Set {prop.Name}", prev, newEl, v => behaviour.Properties[prop.Name] = v));
                };
                return clb;
            }

            ComboBox combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            string[] enumNames = Enum.GetNames(pType);
            for (int i = 0; i < enumNames.Length; i++) combo.Items.Add(enumNames[i]);

            if (behaviour.Properties.TryGetValue(prop.Name, out JsonElement enumEl))
                combo.SelectedItem = enumEl.GetString();
            if (combo.SelectedIndex < 0 && combo.Items.Count > 0)
                combo.SelectedIndex = 0;

            combo.SelectedIndexChanged += (_, _) =>
            {
                string? selected = combo.SelectedItem as string;
                if (selected is null) return;
                JsonElement prev = behaviour.Properties.TryGetValue(prop.Name, out JsonElement prevEl)
                    ? prevEl : default;
                JsonElement newEl = JsonDocument.Parse($"\"{selected}\"").RootElement;
                _context!.Commands.Execute(new SetPropertyCommand<JsonElement>(
                    $"Set {prop.Name}", prev, newEl,
                    v => behaviour.Properties[prop.Name] = v));
            };
            return combo;
        }

        if (pType == typeof(Microsoft.Xna.Framework.Color))
        {
            Microsoft.Xna.Framework.Color current = Microsoft.Xna.Framework.Color.White;
            if (behaviour.Properties.TryGetValue(prop.Name, out JsonElement xnaColorEl)
                && xnaColorEl.ValueKind == JsonValueKind.Object)
            {
                byte r = xnaColorEl.TryGetProperty("R", out JsonElement re) ? (byte)Math.Clamp(re.GetInt32(), 0, 255) : (byte)255;
                byte g = xnaColorEl.TryGetProperty("G", out JsonElement ge) ? (byte)Math.Clamp(ge.GetInt32(), 0, 255) : (byte)255;
                byte b = xnaColorEl.TryGetProperty("B", out JsonElement be) ? (byte)Math.Clamp(be.GetInt32(), 0, 255) : (byte)255;
                byte a = xnaColorEl.TryGetProperty("A", out JsonElement ae) ? (byte)Math.Clamp(ae.GetInt32(), 0, 255) : (byte)255;
                current = new Microsoft.Xna.Framework.Color(r, g, b, a);
            }

            Panel colorRow = new Panel { Height = RowHeight };
            Panel swatch = new Panel
            {
                Dock        = DockStyle.Left,
                Width       = 24,
                Height      = 22,
                BackColor   = System.Drawing.Color.FromArgb(current.A, current.R, current.G, current.B),
                BorderStyle = BorderStyle.FixedSingle,
            };
            Button pickBtn = new Button { Text = "...", Dock = DockStyle.Fill };
            pickBtn.Click += (_, _) =>
            {
                Microsoft.Xna.Framework.Color prev = current;
                if (behaviour.Properties.TryGetValue(prop.Name, out JsonElement prevEl2)
                    && prevEl2.ValueKind == JsonValueKind.Object)
                {
                    byte pr = prevEl2.TryGetProperty("R", out JsonElement pre) ? (byte)Math.Clamp(pre.GetInt32(), 0, 255) : (byte)255;
                    byte pg = prevEl2.TryGetProperty("G", out JsonElement pge) ? (byte)Math.Clamp(pge.GetInt32(), 0, 255) : (byte)255;
                    byte pb = prevEl2.TryGetProperty("B", out JsonElement pbe) ? (byte)Math.Clamp(pbe.GetInt32(), 0, 255) : (byte)255;
                    byte pa = prevEl2.TryGetProperty("A", out JsonElement pae) ? (byte)Math.Clamp(pae.GetInt32(), 0, 255) : (byte)255;
                    prev = new Microsoft.Xna.Framework.Color(pr, pg, pb, pa);
                }

                using RgbaColorPickerDialog dlg = new(prev);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                Microsoft.Xna.Framework.Color chosen = dlg.SelectedColor;
                swatch.BackColor = System.Drawing.Color.FromArgb(chosen.A, chosen.R, chosen.G, chosen.B);
                string json = $"{{\"R\":{chosen.R},\"G\":{chosen.G},\"B\":{chosen.B},\"A\":{chosen.A}}}";
                JsonElement prevJsonEl = behaviour.Properties.TryGetValue(prop.Name, out JsonElement pje) ? pje : default;
                JsonElement newEl = JsonDocument.Parse(json).RootElement;
                _context!.Commands.Execute(new SetPropertyCommand<JsonElement>(
                    $"Set {prop.Name}", prevJsonEl, newEl, v => behaviour.Properties[prop.Name] = v));
            };
            colorRow.Controls.Add(pickBtn);
            colorRow.Controls.Add(swatch);
            return colorRow;
        }

        if (pType == typeof(System.Drawing.Color))
        {
            System.Drawing.Color current = System.Drawing.Color.White;
            if (behaviour.Properties.TryGetValue(prop.Name, out JsonElement colorEl) && colorEl.ValueKind == JsonValueKind.String)
            {
                string? hex = colorEl.GetString();
                if (!string.IsNullOrEmpty(hex))
                    try { current = System.Drawing.ColorTranslator.FromHtml(hex); } catch { }
            }

            Panel colorRow = new Panel { Height = RowHeight };
            Panel swatch = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 24,
                Height    = 22,
                BackColor = current,
                BorderStyle = BorderStyle.FixedSingle,
            };
            Button pickBtn = new Button { Text = "...", Dock = DockStyle.Fill };
            pickBtn.Click += (_, _) =>
            {
                using ColorDialog dlg = new ColorDialog { Color = swatch.BackColor, FullOpen = true };
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                swatch.BackColor = dlg.Color;
                string hex = System.Drawing.ColorTranslator.ToHtml(dlg.Color);
                JsonElement prev = behaviour.Properties.TryGetValue(prop.Name, out JsonElement prevEl) ? prevEl : default;
                JsonElement newEl = JsonDocument.Parse($"\"{hex}\"").RootElement;
                _context!.Commands.Execute(new SetPropertyCommand<JsonElement>(
                    $"Set {prop.Name}", prev, newEl, v => behaviour.Properties[prop.Name] = v));
            };
            colorRow.Controls.Add(pickBtn);
            colorRow.Controls.Add(swatch);
            return colorRow;
        }

        if (pType == typeof(Vector2))
        {
            float vx = 0f, vy = 0f;
            if (behaviour.Properties.TryGetValue(prop.Name, out JsonElement v2El) && v2El.ValueKind == JsonValueKind.Object)
            {
                if (v2El.TryGetProperty("X", out JsonElement xEl)) vx = xEl.GetSingle();
                if (v2El.TryGetProperty("Y", out JsonElement yEl)) vy = yEl.GetSingle();
            }
            return MakeVec2Control(vx, vy, (x, y) =>
            {
                if (_suppressUpdate) return;
                JsonElement prev = behaviour.Properties.TryGetValue(prop.Name, out JsonElement prevEl) ? prevEl : default;
                string json = $"{{\"X\":{x.ToString("R", System.Globalization.CultureInfo.InvariantCulture)},\"Y\":{y.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}}}";
                JsonElement newEl = JsonDocument.Parse(json).RootElement;
                _context!.Commands.Execute(new SetPropertyCommand<JsonElement>(
                    $"Set {prop.Name}", prev, newEl, v => behaviour.Properties[prop.Name] = v));
            });
        }

        if (pType == typeof(Vector3))
        {
            float vx = 0f, vy = 0f, vz = 0f;
            if (behaviour.Properties.TryGetValue(prop.Name, out JsonElement v3El) && v3El.ValueKind == JsonValueKind.Object)
            {
                if (v3El.TryGetProperty("X", out JsonElement xEl)) vx = xEl.GetSingle();
                if (v3El.TryGetProperty("Y", out JsonElement yEl)) vy = yEl.GetSingle();
                if (v3El.TryGetProperty("Z", out JsonElement zEl)) vz = zEl.GetSingle();
            }
            return MakeVec3Control(vx, vy, vz, (x, y, z) =>
            {
                if (_suppressUpdate) return;
                JsonElement prev = behaviour.Properties.TryGetValue(prop.Name, out JsonElement prevEl) ? prevEl : default;
                string json = $"{{\"X\":{x.ToString("R", System.Globalization.CultureInfo.InvariantCulture)},\"Y\":{y.ToString("R", System.Globalization.CultureInfo.InvariantCulture)},\"Z\":{z.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}}}";
                JsonElement newEl = JsonDocument.Parse(json).RootElement;
                _context!.Commands.Execute(new SetPropertyCommand<JsonElement>(
                    $"Set {prop.Name}", prev, newEl, v => behaviour.Properties[prop.Name] = v));
            });
        }

        // Alternativa: cuadro de texto de solo lectura
        TextBox fallback = new TextBox
        {
            ReadOnly  = true,
            Text      = behaviour.Properties.TryGetValue(prop.Name, out JsonElement fallEl)
                        ? fallEl.ToString() : "(unsupported type)",
        };
        return fallback;
    }

    #endregion

    #region Add Behaviour

    private void OnAddBehaviourClick(object? sender, EventArgs e)
    {
        if (_currentObject is null || _registry is null) return;
        using AddBehaviourDialog dlg = new AddBehaviourDialog(_registry, _context?.ActiveProject);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (dlg.SelectedTypeName is null) return;

        EditorBehaviour newBehaviour = new() { TypeName = dlg.SelectedTypeName, Enabled = true };
        _context!.Commands.Execute(new AddBehaviourCommand(_currentObject, newBehaviour));
        _context.EventBus.Publish(new BehaviourAddedEvent(_currentObject, newBehaviour));
        RebuildContent();
    }

    #endregion

    #region Control factories

    private static Label MakeLabel(string text) => new Label
    {
        Text      = text,
        TextAlign = System.Drawing.ContentAlignment.MiddleRight,
        AutoSize  = false,
        Height    = RowHeight,
    };

    private static Label MakeStackedLabel(string text) => new Label
    {
        Text      = text,
        TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
        AutoSize  = false,
        Height    = LabelHeight,
    };

    private Panel MakeFloatControl(float value, float min, float max, Action<float> onChange, EditorPropertyAttribute? attr = null)
    {
        Panel panel = new Panel { Height = RowHeight };
        NumericUpDown num = CreateNumericUpDown((decimal)value,
            (decimal)Math.Max(min, (float)decimal.MinValue),
            (decimal)Math.Min(max, (float)decimal.MaxValue), 3);
        num.Dock = DockStyle.Fill;

        bool hasUserMin = attr is not null && attr.Min != float.MinValue;
        bool hasUserMax = attr is not null && attr.Max != float.MaxValue;

        void UpdateLimitHint()
        {
            bool atLimit = (hasUserMin && (float)num.Value <= min + 0.0001f)
                        || (hasUserMax && (float)num.Value >= max - 0.0001f);
            num.BackColor = atLimit
                ? System.Drawing.Color.FromArgb(255, 200, 200)
                : System.Drawing.SystemColors.Window;
        }

        num.ValueChanged += (_, _) => { onChange((float)num.Value); UpdateLimitHint(); };
        UpdateLimitHint();
        panel.Controls.Add(num);
        return panel;
    }

    private Panel MakeVec2Control(float x, float y, Action<float, float> onChange)
    {
        Panel panel = new Panel { Height = RowHeight };

        Label lx = new Label { Text = "X", Width = 14, Dock = DockStyle.Left, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
        NumericUpDown nx = CreateNumericUpDown((decimal)x, -1_000_000m, 1_000_000m, 3);
        nx.Width = NumericWidth;
        nx.Dock  = DockStyle.Left;

        Label ly = new Label { Text = "Y", Width = 14, Dock = DockStyle.Left, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
        NumericUpDown ny = CreateNumericUpDown((decimal)y, -1_000_000m, 1_000_000m, 3);
        ny.Dock = DockStyle.Fill;

        // Todos los controles se agregan de izquierda a derecha; el último agregado es Fill
        panel.Controls.Add(ny);
        panel.Controls.Add(ly);
        panel.Controls.Add(nx);
        panel.Controls.Add(lx);

        nx.ValueChanged += (_, _) => onChange((float)nx.Value, (float)ny.Value);
        ny.ValueChanged += (_, _) => onChange((float)nx.Value, (float)ny.Value);
        return panel;
    }

    private Panel MakeVec3Control(float x, float y, float z, Action<float, float, float> onChange)
    {
        Panel panel = new Panel { Height = RowHeight };

        Label lx = new Label { Text = "X", Width = 14, Dock = DockStyle.Left, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
        NumericUpDown nx = CreateNumericUpDown((decimal)x, -1_000_000m, 1_000_000m, 3);
        nx.Width = 54;
        nx.Dock  = DockStyle.Left;

        Label ly = new Label { Text = "Y", Width = 14, Dock = DockStyle.Left, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
        NumericUpDown ny = CreateNumericUpDown((decimal)y, -1_000_000m, 1_000_000m, 3);
        ny.Width = 54;
        ny.Dock  = DockStyle.Left;

        Label lz = new Label { Text = "Z", Width = 14, Dock = DockStyle.Left, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
        NumericUpDown nz = CreateNumericUpDown((decimal)z, -1_000_000m, 1_000_000m, 3);
        nz.Dock = DockStyle.Fill;

        panel.Controls.Add(nz);
        panel.Controls.Add(lz);
        panel.Controls.Add(ny);
        panel.Controls.Add(ly);
        panel.Controls.Add(nx);
        panel.Controls.Add(lx);

        nx.ValueChanged += (_, _) => onChange((float)nx.Value, (float)ny.Value, (float)nz.Value);
        ny.ValueChanged += (_, _) => onChange((float)nx.Value, (float)ny.Value, (float)nz.Value);
        nz.ValueChanged += (_, _) => onChange((float)nx.Value, (float)ny.Value, (float)nz.Value);
        return panel;
    }

    private static NumericUpDown CreateNumericUpDown(decimal value, decimal min, decimal max, int decimals)
    {
        NumericUpDown num = new NumericUpDown
        {
            Minimum       = min,
            Maximum       = max,
            DecimalPlaces = decimals,
            Value         = Math.Clamp(value, min, max),
            Increment     = 0.1m,
        };
        return num;
    }

    #endregion

    #region Prefab header

    private Control BuildPrefabHeader(EditorGameObject obj)
    {
        string fileName = Path.GetFileName(obj.PrefabPath ?? string.Empty);

        Panel panel = new Panel
        {
            Height    = 32,
            BackColor = System.Drawing.Color.FromArgb(50, 100, 180),
            Padding   = new System.Windows.Forms.Padding(4, 2, 4, 2),
        };

        Label label = new Label
        {
            Text      = $"Prefab: {fileName}",
            Dock      = DockStyle.Left,
            Width     = 200,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            ForeColor = System.Drawing.Color.White,
        };

        Button applyBtn = new Button
        {
            Text  = "Apply",
            Dock  = DockStyle.Right,
            Width = 55,
        };
        applyBtn.Click += (_, _) =>
        {
            if (obj.PrefabPath is null || _prefabManager is null) return;
            _context!.Commands.Execute(new ApplyPrefabCommand(obj, obj.PrefabPath, _prefabManager));
        };

        Button revertBtn = new Button
        {
            Text  = "Revert",
            Dock  = DockStyle.Right,
            Width = 55,
        };
        revertBtn.Click += (_, _) =>
        {
            if (obj.PrefabPath is null || _prefabManager is null) return;
            _context!.Commands.Execute(new RevertPrefabCommand(obj, obj.PrefabPath, _prefabManager));
            RebuildContent();
        };

        panel.Controls.Add(applyBtn);
        panel.Controls.Add(revertBtn);
        panel.Controls.Add(label);
        return panel;
    }

    #endregion

    #region Helpers

    private static string ExtractShortName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return "(unknown)";
        int dot = typeName.LastIndexOf('.');
        return dot >= 0 ? typeName[(dot + 1)..] : typeName;
    }

    #endregion
}
