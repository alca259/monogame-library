namespace MonoGame.Editor.Winforms.Infrastructure;

/// <summary>Marshals work onto the UI thread via the captured <see cref="SynchronizationContext"/>.</summary>
internal static class UiDispatcher
{
    private static SynchronizationContext? _context;

    /// <summary>
    /// Captures the current synchronization context. Must be called once from the UI thread
    /// before starting any background work.
    /// </summary>
    internal static void Capture()
    {
        if (SynchronizationContext.Current is null)
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

        _context = SynchronizationContext.Current;
    }

    /// <summary>Posts <paramref name="action"/> to execute on the UI thread.</summary>
    internal static void Post(Action action) =>
        _context!.Post(_ => action(), null);
}
