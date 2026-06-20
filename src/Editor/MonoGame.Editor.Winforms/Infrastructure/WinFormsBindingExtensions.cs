using System.ComponentModel;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.Input;

namespace MonoGame.Editor.Winforms.Infrastructure;

/// <summary>
/// Helpers de binding bidireccional entre controles WinForms e implementaciones de
/// <see cref="INotifyPropertyChanged"/> (ViewModels).
/// </summary>
internal static class WinFormsBindingExtensions
{
    // ── Propiedades ──────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un <see cref="Binding"/> entre la propiedad <paramref name="controlProperty"/> del
    /// control y la propiedad <paramref name="vmProperty"/> del ViewModel.
    /// </summary>
    public static Binding BindProperty(
        this Control control,
        string controlProperty,
        object dataSource,
        string vmProperty,
        DataSourceUpdateMode updateMode = DataSourceUpdateMode.OnPropertyChanged)
    {
        Binding binding = new(controlProperty, dataSource, vmProperty, false, updateMode);
        control.DataBindings.Add(binding);
        return binding;
    }

    // ── Comandos ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Enlaza un <see cref="ButtonBase"/> a un <see cref="IRelayCommand"/>: el clic
    /// ejecuta el comando y <see cref="ButtonBase.Enabled"/> sigue <c>CanExecute</c>.
    /// </summary>
    public static void BindCommand(this ButtonBase button, IRelayCommand command)
    {
        button.Click += (_, _) => command.Execute(null);
        command.CanExecuteChanged += (_, _) => button.Enabled = command.CanExecute(null);
        button.Enabled = command.CanExecute(null);
    }

    /// <summary>
    /// Enlaza un <see cref="ToolStripItem"/> a un <see cref="IRelayCommand"/>.
    /// </summary>
    public static void BindCommand(this ToolStripItem item, IRelayCommand command)
    {
        item.Click += (_, _) => command.Execute(null);
        command.CanExecuteChanged += (_, _) => item.Enabled = command.CanExecute(null);
        item.Enabled = command.CanExecute(null);
    }

    // ── Visibilidad ──────────────────────────────────────────────────────────

    /// <summary>
    /// Mantiene <see cref="Control.Visible"/> sincronizado con una propiedad booleana de la VM.
    /// </summary>
    public static void BindVisible(this Control control, INotifyPropertyChanged vm, string vmProperty)
    {
        void Sync()
        {
            object? value = vm.GetType().GetProperty(vmProperty)?.GetValue(vm);
            if (value is bool b)
                control.Visible = b;
        }

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == vmProperty || e.PropertyName is null)
                Sync();
        };
        Sync();
    }

    /// <summary>
    /// Mantiene <see cref="Control.Enabled"/> sincronizado con una propiedad booleana de la VM.
    /// </summary>
    public static void BindEnabled(this Control control, INotifyPropertyChanged vm, string vmProperty)
    {
        void Sync()
        {
            object? value = vm.GetType().GetProperty(vmProperty)?.GetValue(vm);
            if (value is bool b)
                control.Enabled = b;
        }

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == vmProperty || e.PropertyName is null)
                Sync();
        };
        Sync();
    }
}
