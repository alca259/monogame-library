namespace MonoGame.Editor.Core.Input;

/// <summary>Representación en el editor de una acción de entrada con nombre y sus enlaces.</summary>
public sealed class InputActionEntry
{
    /// <summary>Obtiene o establece el nombre único de la acción.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Obtiene la lista de enlaces de esta acción.</summary>
    public List<InputBindingEntry> Bindings { get; } = [];
}
