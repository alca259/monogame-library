using System.Text.Json;

namespace MonoGame.Editor.Maui.Drawers;

/// <summary>
/// Clase base para editores personalizados de Behaviour en el Inspector.
/// Decorar la subclase con <see cref="MonoGame.Editor.Core.Attributes.CustomBehaviourEditorAttribute"/>
/// para que <see cref="BehaviourEditorRegistry"/> la registre automáticamente.
/// </summary>
public abstract class BehaviourEditor
{
    /// <summary>Navegación MAUI activa. Se asigna desde el Inspector antes de llamar a <see cref="BuildInspector"/>.</summary>
    internal INavigation? Navigation { get; set; }

    /// <summary>Ruta absoluta a la raíz del proyecto activo. Se asigna desde el Inspector antes de llamar a <see cref="BuildInspector"/>.</summary>
    internal string? ProjectRootPath { get; set; }

    /// <summary>Construye la UI completa del cuerpo de la tarjeta para el Behaviour dado.</summary>
    public abstract View BuildInspector(EditorBehaviour behaviour, EditorGameObject owner);

    // ── Helpers de control ────────────────────────────────────────────────────

    /// <summary>Campo numérico como slider con rango definido.</summary>
    protected static View BuildSliderField(string label, double value, double min, double max,
        Action<double> onChange, bool readOnly = false, string? textColor = null, string? bgColor = null)
        => PropertyControlHelper.BuildSliderField(label, value, min, max, onChange, readOnly, textColor, bgColor);

    /// <summary>Campo numérico con stepper.</summary>
    protected static View BuildNumberField(string label, double value, Action<double> onChange,
        bool readOnly = false, string? textColor = null, string? bgColor = null)
        => PropertyControlHelper.BuildNumberField(label, value, onChange, readOnly, textColor, bgColor);

    /// <summary>Campo de texto de una línea.</summary>
    protected static View BuildTextField(string label, string value, Action<string> onChange,
        bool readOnly = false, string? textColor = null, string? bgColor = null)
        => PropertyControlHelper.BuildTextField(label, value, onChange, readOnly, textColor, bgColor);

    /// <summary>CheckBox para propiedades booleanas.</summary>
    protected static View BuildBoolField(string label, bool value, Action<bool> onChange,
        bool readOnly = false)
        => PropertyControlHelper.BuildBoolField(label, value, onChange, readOnly);

    /// <summary>Swatch + picker para propiedades <see cref="Microsoft.Xna.Framework.Color"/> serializadas como RGBA.</summary>
    protected static View BuildColorField(string label, JsonElement value, Action<JsonElement> onChange)
        => PropertyControlHelper.BuildColorField(label, value, onChange);

    /// <summary>Entry de solo lectura + botón "…" que abre <c>RelativePathPickerDialog</c>.</summary>
    protected View BuildFilePickerField(string label, string value, string[]? extensions,
        Action<string> onChange, bool readOnly = false)
        => PropertyControlHelper.BuildFilePickerField(label, value, Navigation,
            ProjectRootPath ?? string.Empty, extensions, onChange, readOnly);

    /// <summary>Separador de sección con título.</summary>
    protected static View BuildHeaderSeparator(string title)
        => PropertyControlHelper.BuildHeaderSeparator(title);

    /// <summary>Fila etiqueta + control con colores opcionales en la etiqueta.</summary>
    protected static View BuildPropertyRow(string labelText, View control,
        string? textColor = null, string? bgColor = null)
        => PropertyControlHelper.BuildPropertyRow(labelText, control, textColor, bgColor);

    /// <summary>Ejecuta un cambio de propiedad con soporte de deshacer/rehacer.</summary>
    protected static void SetProperty(EditorBehaviour behaviour, string key, JsonElement newValue)
        => PropertyControlHelper.SetProperty(behaviour, key, newValue);
}
