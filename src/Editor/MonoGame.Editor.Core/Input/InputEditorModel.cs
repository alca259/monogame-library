using Alca.MonoGame.Kernel.Input;

namespace MonoGame.Editor.Core.Input;

/// <summary>
/// Modelo de editor para un archivo <c>*.input.json</c>. Mantiene una lista mutable de acciones y enlaces,
/// y serializa/deserializa en el mismo formato JSON utilizado por <see cref="Alca.MonoGame.Kernel.Input.InputSerializer"/>.
/// </summary>
public sealed class InputEditorModel
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly List<InputActionEntry> _actions = [];

    /// <summary>Obtiene la ruta absoluta al archivo de respaldo.</summary>
    public string FilePath { get; }

    /// <summary>Obtiene la lista ordenada de acciones.</summary>
    public IReadOnlyList<InputActionEntry> Actions => _actions;

    private InputEditorModel(string filePath) => FilePath = filePath;

    /// <summary>Carga o crea un modelo desde <paramref name="filePath"/>. Devuelve un modelo vacío si el archivo no existe.</summary>
    public static async Task<InputEditorModel> LoadAsync(string filePath)
    {
        InputEditorModel model = new(filePath);
        if (!File.Exists(filePath)) return model;

        await using FileStream fs = File.OpenRead(filePath);
        ActionMapDto? dto = await JsonSerializer.DeserializeAsync<ActionMapDto>(fs, _jsonOptions).ConfigureAwait(false);
        if (dto is null) return model;

        foreach (ActionDto actionDto in dto.Actions)
        {
            InputActionEntry entry = new() { Name = actionDto.Name };
            foreach (BindingDto b in actionDto.Bindings)
                entry.Bindings.Add(new InputBindingEntry(b.DeviceType, b.Code));
            model._actions.Add(entry);
        }

        return model;
    }

    /// <summary>Serializa el modelo en <see cref="FilePath"/>.</summary>
    public async Task SaveAsync()
    {
        ActionDto[] actionDtos = new ActionDto[_actions.Count];
        for (int i = 0; i < _actions.Count; i++)
        {
            InputActionEntry entry = _actions[i];
            BindingDto[] bindings = new BindingDto[entry.Bindings.Count];
            for (int j = 0; j < entry.Bindings.Count; j++)
                bindings[j] = new BindingDto { DeviceType = entry.Bindings[j].DeviceType, Code = entry.Bindings[j].Code };
            actionDtos[i] = new ActionDto { Name = entry.Name, Bindings = bindings };
        }

        string? dir = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        await using FileStream fs = File.Create(FilePath);
        await JsonSerializer.SerializeAsync(fs, new ActionMapDto { Actions = actionDtos }, _jsonOptions).ConfigureAwait(false);
    }

    /// <summary>Agrega una nueva acción. No hace nada si ya existe una acción con ese nombre.</summary>
    public void AddAction(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || _actions.Exists(a => a.Name == name)) return;
        _actions.Add(new InputActionEntry { Name = name });
    }

    /// <summary>Elimina la acción con el nombre indicado. Devuelve <c>false</c> si no se encuentra.</summary>
    public bool RemoveAction(string name)
    {
        int idx = _actions.FindIndex(a => a.Name == name);
        if (idx < 0) return false;
        _actions.RemoveAt(idx);
        return true;
    }

    /// <summary>Devuelve la entrada de acción con el nombre indicado, o <c>null</c>.</summary>
    public InputActionEntry? GetAction(string name) => _actions.Find(a => a.Name == name);

    /// <summary>Agrega un enlace a la acción indicada. Ignora duplicados.</summary>
    public void AddBinding(string actionName, DeviceType device, int code)
    {
        InputActionEntry? entry = GetAction(actionName);
        if (entry is null) return;
        InputBindingEntry binding = new(device, code);
        if (!entry.Bindings.Contains(binding))
            entry.Bindings.Add(binding);
    }

    /// <summary>Elimina un enlace de la acción indicada.</summary>
    public void RemoveBinding(string actionName, DeviceType device, int code) =>
        GetAction(actionName)?.Bindings.Remove(new InputBindingEntry(device, code));

    // ── DTOs de JSON (coinciden con el formato InputSerializer del Kernel) ──

    private sealed class ActionMapDto
    {
        public ActionDto[] Actions { get; set; } = [];
    }

    private sealed class ActionDto
    {
        public string Name { get; set; } = string.Empty;
        public BindingDto[] Bindings { get; set; } = [];
    }

    private sealed class BindingDto
    {
        public DeviceType DeviceType { get; set; }
        public int Code { get; set; }
    }
}
