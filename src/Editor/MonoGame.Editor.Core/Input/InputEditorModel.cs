using Alca.MonoGame.Kernel.Input;

namespace MonoGame.Editor.Core.Input;

/// <summary>
/// Editor model for a <c>*.input.json</c> file. Maintains a mutable list of actions and bindings
/// and serializes/deserializes in the same JSON format used by <see cref="Alca.MonoGame.Kernel.Input.InputSerializer"/>.
/// </summary>
public sealed class InputEditorModel
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly List<InputActionEntry> _actions = [];

    /// <summary>Gets the absolute path to the backing file.</summary>
    public string FilePath { get; }

    /// <summary>Gets the ordered list of actions.</summary>
    public IReadOnlyList<InputActionEntry> Actions => _actions;

    private InputEditorModel(string filePath) => FilePath = filePath;

    /// <summary>Loads or creates a model from <paramref name="filePath"/>. Returns an empty model if the file does not exist.</summary>
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

    /// <summary>Serializes the model to <see cref="FilePath"/>.</summary>
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

    /// <summary>Adds a new action. Does nothing if an action with that name already exists.</summary>
    public void AddAction(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || _actions.Exists(a => a.Name == name)) return;
        _actions.Add(new InputActionEntry { Name = name });
    }

    /// <summary>Removes the action with the given name. Returns <c>false</c> if not found.</summary>
    public bool RemoveAction(string name)
    {
        int idx = _actions.FindIndex(a => a.Name == name);
        if (idx < 0) return false;
        _actions.RemoveAt(idx);
        return true;
    }

    /// <summary>Returns the action entry with the given name, or <c>null</c>.</summary>
    public InputActionEntry? GetAction(string name) => _actions.Find(a => a.Name == name);

    /// <summary>Adds a binding to the named action. Ignores duplicates.</summary>
    public void AddBinding(string actionName, DeviceType device, int code)
    {
        InputActionEntry? entry = GetAction(actionName);
        if (entry is null) return;
        InputBindingEntry binding = new(device, code);
        if (!entry.Bindings.Contains(binding))
            entry.Bindings.Add(binding);
    }

    /// <summary>Removes a binding from the named action.</summary>
    public void RemoveBinding(string actionName, DeviceType device, int code) =>
        GetAction(actionName)?.Bindings.Remove(new InputBindingEntry(device, code));

    // ── JSON DTOs (match Kernel InputSerializer format) ───────────────────

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
