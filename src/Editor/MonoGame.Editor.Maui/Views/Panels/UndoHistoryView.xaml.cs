namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Dock tab "History". Shows undo stack (oldest→newest) above a blue divider,
/// redo stack (greyed) below. Clear button empties both stacks.
/// </summary>
public sealed partial class UndoHistoryView : ContentView
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;

    private static readonly Color UndoEntryColor = Color.FromArgb("#E6E6E8");
    private static readonly Color RedoEntryColor  = Color.FromArgb("#6A6A72");
    private static readonly Color SeparatorColor  = Color.FromArgb("#3A7BCC");

    private Action<UndoPerformedEvent>? _onUndo;
    private Action<RedoPerformedEvent>? _onRedo;

    public UndoHistoryView()
    {
        InitializeComponent();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) Subscribe();
        else Unsubscribe();
    }

    private void Subscribe()
    {
        _onUndo = _ => MainThread.BeginInvokeOnMainThread(RebuildHistory);
        _onRedo = _ => MainThread.BeginInvokeOnMainThread(RebuildHistory);
        _bus.Subscribe(_onUndo);
        _bus.Subscribe(_onRedo);
    }

    private void Unsubscribe()
    {
        if (_onUndo is not null) _bus.Unsubscribe(_onUndo);
        if (_onRedo is not null) _bus.Unsubscribe(_onRedo);
    }

    private void RebuildHistory()
    {
        HistoryStack.Children.Clear();

        CommandStack commands = EditorContext.Instance.Commands;
        IReadOnlyList<string> undos = commands.GetUndoDescriptions();
        IReadOnlyList<string> redos = commands.GetRedoDescriptions();

        // GetUndoDescriptions returns newest→oldest; display oldest→newest
        for (int i = undos.Count - 1; i >= 0; i--)
        {
            HistoryStack.Children.Add(new Label
            {
                Text      = undos[i],
                TextColor = UndoEntryColor,
                FontSize  = 11,
                Padding   = new Thickness(10, 3),
            });
        }

        HistoryStack.Children.Add(new BoxView
        {
            Color         = SeparatorColor,
            HeightRequest = 2,
            Margin        = new Thickness(0, 2),
        });

        foreach (string desc in redos)
        {
            HistoryStack.Children.Add(new Label
            {
                Text      = desc,
                TextColor = RedoEntryColor,
                FontSize  = 11,
                Padding   = new Thickness(10, 3),
            });
        }

        HistorySummaryLabel.Text = $"{undos.Count} / {commands.MaxHistory}";
    }

    private void OnClearClicked(object sender, EventArgs e)
    {
        EditorContext.Instance.Commands.Clear();
        RebuildHistory();
    }
}
