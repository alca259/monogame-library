using SDColor = System.Drawing.Color;

namespace MonoGame.Editor.WinForms.Panels;

/// <summary>
/// Muestra el historial de deshacer/rehacer y permite al usuario navegar a cualquier punto
/// haciendo clic en una entrada. Las entradas de deshacer se muestran arriba; las de rehacer debajo
/// de un separador "── redo ──".
/// </summary>
public sealed class UndoHistoryPanel : Panel
{
    private const string RedoSeparatorText = "── redo ──";

    private readonly ListBox _listBox;
    private EditorContext?   _context;
    private IEditorEventBus? _eventBus;

    /// <summary>Inicializa una nueva instancia de <see cref="UndoHistoryPanel"/>.</summary>
    public UndoHistoryPanel()
    {
        _listBox = new ListBox
        {
            Dock          = DockStyle.Fill,
            DrawMode      = DrawMode.OwnerDrawFixed,
            ItemHeight    = 18,
            IntegralHeight = false,
            Font          = new System.Drawing.Font("Segoe UI", 8.5f),
        };

        _listBox.DrawItem      += OnDrawItem;
        _listBox.MouseClick    += OnListMouseClick;

        Controls.Add(_listBox);
    }

    /// <summary>Conecta el panel con el contexto del editor y el bus de eventos.</summary>
    public void Initialize(EditorContext context, IEditorEventBus eventBus)
    {
        _context  = context;
        _eventBus = eventBus;
        eventBus.Subscribe<UndoPerformedEvent>(OnHistoryChanged);
        eventBus.Subscribe<RedoPerformedEvent>(OnHistoryChanged);
    }

    /// <summary>Reconstruye la lista a partir del estado actual de la pila de comandos.</summary>
    public void Refresh(CommandStack commands)
    {
        if (InvokeRequired) { BeginInvoke(() => Refresh(commands)); return; }

        _listBox.BeginUpdate();
        _listBox.Items.Clear();

        IReadOnlyList<string> undos = commands.GetUndoDescriptions();
        IReadOnlyList<string> redos = commands.GetRedoDescriptions();

        for (int i = 0; i < undos.Count; i++)
            _listBox.Items.Add(new HistoryItem(undos[i], ItemKind.Undo, i));

        if (redos.Count > 0)
        {
            _listBox.Items.Add(new HistoryItem(RedoSeparatorText, ItemKind.Separator, -1));
            for (int i = 0; i < redos.Count; i++)
                _listBox.Items.Add(new HistoryItem(redos[i], ItemKind.Redo, i));
        }

        _listBox.EndUpdate();
    }

    private void OnHistoryChanged(IEditorEvent _)
    {
        if (_context is null) return;
        Refresh(_context.Commands);
    }

    private void OnListMouseClick(object? sender, MouseEventArgs e)
    {
        if (_context is null) return;
        int idx = _listBox.IndexFromPoint(e.Location);
        if (idx < 0 || idx >= _listBox.Items.Count) return;

        if (_listBox.Items[idx] is not HistoryItem item) return;
        if (item.Kind == ItemKind.Separator) return;

        // Deshacer N+1 veces para alcanzar el punto del historial clicado
        if (item.Kind == ItemKind.Undo)
        {
            int times = item.Index + 1;
            for (int i = 0; i < times; i++)
                _context.Commands.Undo();
        }
        else
        {
            int times = item.Index + 1;
            for (int i = 0; i < times; i++)
                _context.Commands.Redo();
        }

        Refresh(_context.Commands);
    }

    private void OnDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _listBox.Items.Count) return;

        e.DrawBackground();

        if (_listBox.Items[e.Index] is not HistoryItem item) return;

        SDColor fg = item.Kind switch
        {
            ItemKind.Redo      => SDColor.FromArgb(160, 160, 255),
            ItemKind.Separator => SDColor.Gray,
            _                  => e.ForeColor,
        };

        System.Drawing.Font font = item.Kind == ItemKind.Separator
            ? new System.Drawing.Font(e.Font!, System.Drawing.FontStyle.Italic)
            : e.Font!;

        using System.Drawing.SolidBrush brush = new(fg);
        e.Graphics.DrawString(item.Label, font, brush, e.Bounds);

        if (item.Kind == ItemKind.Separator)
            font.Dispose();

        e.DrawFocusRectangle();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _listBox.DrawItem   -= OnDrawItem;
            _listBox.MouseClick -= OnListMouseClick;
            if (_eventBus is not null)
            {
                _eventBus.Unsubscribe<UndoPerformedEvent>(OnHistoryChanged);
                _eventBus.Unsubscribe<RedoPerformedEvent>(OnHistoryChanged);
            }
        }
        base.Dispose(disposing);
    }

    private enum ItemKind { Undo, Redo, Separator }

    private sealed class HistoryItem(string label, ItemKind kind, int index)
    {
        public string   Label { get; } = label;
        public ItemKind Kind  { get; } = kind;
        public int      Index { get; } = index;
        public override string ToString() => Label;
    }
}
