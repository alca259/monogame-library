using System.Text.Json;
using System.Windows.Forms;

namespace MonoGame.Editor.Winforms.Drawers;

/// <summary>
/// Clase base para editores personalizados de Behaviour en el Inspector WinForms.
/// Decorar la subclase con <see cref="MonoGame.Editor.Core.Attributes.CustomBehaviourEditorAttribute"/>
/// para que <see cref="BehaviourEditorRegistry"/> la registre automáticamente.
/// </summary>
public abstract class BehaviourEditor
{
    /// <summary>Ruta absoluta al proyecto activo. Se asigna antes de llamar a <see cref="BuildInspector"/>.</summary>
    internal string? ProjectRootPath { get; set; }

    /// <summary>Construye la UI del cuerpo de la tarjeta para el Behaviour dado.</summary>
    public abstract Control BuildInspector(EditorBehaviour behaviour, EditorGameObject owner);

    /// <summary>
    /// Nombres de propiedades float que el viewport visualiza como círculos de radio.
    /// Sobreescribir en editores built-in para tipos del NuGet sin <c>[EditorRadiusPreview]</c>.
    /// </summary>
    public virtual IReadOnlyList<string> RadiusPreviewProperties => [];

    // ── Helpers de control (delegan en PropertyControlHelper) ─────────────────

    /// <summary>Fila con slider para un rango [min, max].</summary>
    protected static Control BuildSliderField(string label, double value, double min, double max,
        Action<double> onChange, bool readOnly = false)
        => PropertyControlHelper.BuildSliderField(label, value, min, max, onChange, readOnly);

    /// <summary>Fila con stepper numérico.</summary>
    protected static Control BuildNumberField(string label, double value,
        Action<double> onChange, bool readOnly = false)
        => PropertyControlHelper.BuildNumberField(label, value, onChange, readOnly);

    /// <summary>Fila con campo de texto.</summary>
    protected static Control BuildTextField(string label, string value,
        Action<string> onChange, bool readOnly = false)
        => PropertyControlHelper.BuildTextField(label, value, onChange, readOnly);

    /// <summary>Fila con checkbox.</summary>
    protected static Control BuildBoolField(string label, bool value,
        Action<bool> onChange, bool readOnly = false)
        => PropertyControlHelper.BuildBoolField(label, value, onChange, readOnly);

    /// <summary>Fila con swatch de color y picker.</summary>
    protected static Control BuildColorField(string label, JsonElement value,
        Action<JsonElement> onChange)
        => PropertyControlHelper.BuildColorField(label, value, onChange);

    /// <summary>Fila con path label + botón "…" para seleccionar un fichero.</summary>
    protected Control BuildFilePickerField(string label, string value,
        string[]? extensions, Action<string> onChange, bool readOnly = false)
        => PropertyControlHelper.BuildFilePickerField(
            label, value, ProjectRootPath ?? string.Empty, extensions, onChange, readOnly);

    /// <summary>Separador de sección con título.</summary>
    protected static Control BuildHeaderSeparator(string title)
        => PropertyControlHelper.BuildHeaderSeparator(title);

    /// <summary>Fila con dos steppers X/Y para un Vector2 JSON.</summary>
    protected static Control BuildVector2Field(string label, JsonElement el,
        Action<double> onX, Action<double> onY, bool readOnly = false)
    {
        (double x, double y) = PropertyControlHelper.GetVector2(el);
        return PropertyControlHelper.BuildVector2Field(label, x, y, onX, onY, readOnly);
    }

    /// <summary>Serializa (x, y) como JsonElement {X, Y}.</summary>
    protected static JsonElement SerializeVector2(double x, double y)
        => PropertyControlHelper.SerializeVector2(x, y);

    /// <summary>Fila con tres steppers X/Y/Z para un Vector3 JSON.</summary>
    protected static Control BuildVector3Field(string label, JsonElement el,
        Action<double> onX, Action<double> onY, Action<double> onZ, bool readOnly = false)
    {
        (double x, double y, double z) = PropertyControlHelper.GetVector3(el);
        return PropertyControlHelper.BuildVector3Field(label, x, y, z, onX, onY, onZ, readOnly);
    }

    /// <summary>Serializa (x, y, z) como JsonElement {X, Y, Z}.</summary>
    protected static JsonElement SerializeVector3(double x, double y, double z)
        => PropertyControlHelper.SerializeVector3(x, y, z);

    /// <summary>Aplica un cambio de propiedad con soporte de undo/redo.</summary>
    protected static void SetProperty(EditorBehaviour behaviour, string key, JsonElement newValue)
        => PropertyControlHelper.SetProperty(behaviour, key, newValue);

    /// <summary>Ensambla una lista de filas en un Panel vertical listo para la tarjeta.</summary>
    protected static Control BuildCard(IReadOnlyList<Control> rows)
        => PropertyControlHelper.BuildCard(rows);
}
