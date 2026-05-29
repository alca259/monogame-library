namespace MonoGame.Editor.WinForms.Panels;

/// <summary>
/// Panel de consola con salida de registro codificada por colores, filtrado por nivel, copiado y análisis de líneas compatible con MSBuild.
/// </summary>
public sealed class ConsolePanel : UserControl
{
    #region Fields

    private readonly ToolStrip             _toolbar;
    private readonly ToolStripButton       _clearBtn;
    private readonly ToolStripButton       _copyBtn;
    private readonly ToolStripDropDownButton _filterBtn;
    private readonly ToolStripMenuItem     _filterAll;
    private readonly ToolStripMenuItem     _filterDebug;
    private readonly ToolStripMenuItem     _filterInfo;
    private readonly ToolStripMenuItem     _filterWarning;
    private readonly ToolStripMenuItem     _filterError;
    private readonly RichTextBox           _output;

    private LogLevel? _activeFilter;
    private readonly List<(string text, System.Drawing.Color color, bool bold)> _entries = [];
    private System.Drawing.Font? _boldFont;

    private EditorContext? _context;
    private Action<LogEntryAddedEvent>? _onLogEntry;

    #endregion

    #region Constructor

    /// <summary>Construye el panel de consola. Llama a <see cref="Initialize"/> para conectar con el bus de eventos.</summary>
    public ConsolePanel()
    {
        _clearBtn = new ToolStripButton("Clear") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _copyBtn  = new ToolStripButton("Copy")  { DisplayStyle = ToolStripItemDisplayStyle.Text };

        _filterAll     = new ToolStripMenuItem("All");
        _filterDebug   = new ToolStripMenuItem("Debug");
        _filterInfo    = new ToolStripMenuItem("Info");
        _filterWarning = new ToolStripMenuItem("Warning");
        _filterError   = new ToolStripMenuItem("Error");

        _filterBtn = new ToolStripDropDownButton("Filter ▼") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _filterBtn.DropDownItems.AddRange(new ToolStripItem[]
        {
            _filterAll, _filterDebug, _filterInfo, _filterWarning, _filterError,
        });

        _toolbar = new ToolStrip { Dock = DockStyle.Top, Height = 28 };
        _toolbar.Items.Add(_clearBtn);
        _toolbar.Items.Add(_copyBtn);
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(_filterBtn);

        _output = new RichTextBox
        {
            Dock        = DockStyle.Fill,
            ReadOnly    = true,
            BackColor   = System.Drawing.Color.FromArgb(30, 30, 30),
            ForeColor   = System.Drawing.SystemColors.ControlText,
            Font        = new System.Drawing.Font("Consolas", 9f),
            BorderStyle = BorderStyle.None,
            ScrollBars  = RichTextBoxScrollBars.Vertical,
        };

        Controls.Add(_output);
        Controls.Add(_toolbar);

        _clearBtn.Click      += (_, _) => Clear();
        _copyBtn.Click       += OnCopy;
        _filterAll.Click     += (_, _) => SetFilter(null);
        _filterDebug.Click   += (_, _) => SetFilter(LogLevel.Debug);
        _filterInfo.Click    += (_, _) => SetFilter(LogLevel.Info);
        _filterWarning.Click += (_, _) => SetFilter(LogLevel.Warning);
        _filterError.Click   += (_, _) => SetFilter(LogLevel.Error);
    }

    #endregion

    #region Initialization

    /// <summary>Se suscribe a <see cref="LogEntryAddedEvent"/> en el bus de eventos compartido.</summary>
    public void Initialize(EditorContext context)
    {
        _context  = context;
        _onLogEntry = evt => AppendLine(evt.Entry.Message, evt.Entry.Level);
        _context.EventBus.Subscribe(_onLogEntry);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _context is not null && _onLogEntry is not null)
            _context.EventBus.Unsubscribe(_onLogEntry);
        if (disposing)
        {
            _boldFont?.Dispose();
            _boldFont = null;
        }
        base.Dispose(disposing);
    }

    #endregion

    #region Public API

    /// <summary>Agrega una línea de registro (seguro para hilos).</summary>
    public void AppendLine(string message, LogLevel level = LogLevel.Info)
    {
        if (InvokeRequired) { BeginInvoke(() => AppendLine(message, level)); return; }
        string text  = FormatLine(message, level);
        bool   bold  = level == LogLevel.Error;
        System.Drawing.Color color = LevelColor(level);
        _entries.Add((text, color, bold));
        if (!_activeFilter.HasValue || _activeFilter.Value == level)
            AppendColoredLine(text, color, bold);
    }

    /// <summary>Analiza los patrones de salida de MSBuild y les asigna colores según corresponda (seguro para hilos).</summary>
    public void AppendBuildLine(string line)
    {
        if (InvokeRequired) { BeginInvoke(() => AppendBuildLine(line)); return; }

        LogLevel level;
        System.Drawing.Color color;

        if (line.Contains("error CS", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
        {
            level = LogLevel.Error;
            color = System.Drawing.Color.IndianRed;
        }
        else if (line.Contains("warning CS", StringComparison.OrdinalIgnoreCase)
                 || line.StartsWith("Warning", StringComparison.OrdinalIgnoreCase))
        {
            level = LogLevel.Warning;
            color = System.Drawing.Color.Goldenrod;
        }
        else if (line.Contains("Build succeeded", StringComparison.OrdinalIgnoreCase))
        {
            level = LogLevel.Info;
            color = System.Drawing.Color.LightGreen;
        }
        else if (line.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase))
        {
            level = LogLevel.Error;
            color = System.Drawing.Color.IndianRed;
        }
        else
        {
            level = LogLevel.Info;
            color = System.Drawing.SystemColors.ControlText;
        }

        bool bold = level == LogLevel.Error;
        _entries.Add((line, color, bold));
        if (!_activeFilter.HasValue || _activeFilter.Value == level)
            AppendColoredLine(line, color, bold);
    }

    /// <summary>Borra toda la salida (seguro para hilos).</summary>
    public void Clear()
    {
        if (InvokeRequired) { BeginInvoke(Clear); return; }
        _entries.Clear();
        _output.Clear();
    }

    #endregion

    #region Internal

    private void SetFilter(LogLevel? level)
    {
        _activeFilter  = level;
        _filterBtn.Text = level.HasValue ? $"Filter: {level.Value} ▼" : "Filter ▼";
        RebuildOutput();
    }

    private void RebuildOutput()
    {
        _output.Clear();
        for (int i = 0; i < _entries.Count; i++)
        {
            (string text, System.Drawing.Color color, bool bold) = _entries[i];
            // Inferir el nivel a partir del color para respetar el filtro — almacenar el nivel por separado para mayor precisión
            if (_activeFilter.HasValue && !EntryMatchesFilter(_entries[i].text))
                continue;
            AppendColoredLine(text, color, bold);
        }
    }

    private bool EntryMatchesFilter(string text)
    {
        if (!_activeFilter.HasValue) return true;
        string levelTag = _activeFilter.Value switch
        {
            LogLevel.Debug   => "[DEBUG]",
            LogLevel.Info    => "[INFO]",
            LogLevel.Warning => "[WARN]",
            LogLevel.Error   => "[ERROR]",
            _                => string.Empty,
        };
        return text.Contains(levelTag, StringComparison.OrdinalIgnoreCase);
    }

    private void AppendColoredLine(string text, System.Drawing.Color color, bool bold)
    {
        _output.SelectionStart  = _output.TextLength;
        _output.SelectionLength = 0;
        _output.SelectionColor  = color;
        if (bold)
        {
            _boldFont ??= new System.Drawing.Font(_output.Font, System.Drawing.FontStyle.Bold);
            _output.SelectionFont = _boldFont;
        }
        _output.AppendText(text + Environment.NewLine);
        _output.SelectionFont  = _output.Font;
        _output.SelectionColor = _output.ForeColor;
        _output.ScrollToCaret();
    }

    private static string FormatLine(string message, LogLevel level)
    {
        string tag = level switch
        {
            LogLevel.Debug   => "DEBUG",
            LogLevel.Info    => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error   => "ERROR",
            _                => "INFO",
        };
        return $"[{DateTime.Now:HH:mm:ss}] [{tag}] {message}";
    }

    private static System.Drawing.Color LevelColor(LogLevel level) => level switch
    {
        LogLevel.Debug   => System.Drawing.Color.DimGray,
        LogLevel.Warning => System.Drawing.Color.Goldenrod,
        LogLevel.Error   => System.Drawing.Color.IndianRed,
        _                => System.Drawing.SystemColors.ControlText,
    };

    private void OnCopy(object? sender, EventArgs e)
    {
        string text = string.IsNullOrEmpty(_output.SelectedText) ? _output.Text : _output.SelectedText;
        if (!string.IsNullOrEmpty(text))
            Clipboard.SetText(text);
    }

    #endregion
}
