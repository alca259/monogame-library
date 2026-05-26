using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alca.MonoGame.Kernel.Input;

/// <summary>Serializes and deserializes an <see cref="InputActionMap"/> to and from a JSON file.</summary>
public sealed class InputSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Saves the action map to the specified file path as JSON. Must be awaited; apply the result on the main thread.</summary>
    /// <param name="map">The action map to serialize.</param>
    /// <param name="filePath">Destination file path.</param>
    public async Task Save(InputActionMap map, string filePath)
    {
        IReadOnlyList<InputAction> actions = map.GetAllActions();
        ActionDto[] actionDtos = new ActionDto[actions.Count];

        for (int i = 0; i < actions.Count; i++)
        {
            actionDtos[i] = new ActionDto
            {
                Name = actions[i].Name,
                Bindings = actions[i].GetBindings()
            };
        }

        ActionMapDto dto = new() { Actions = actionDtos };

        await using FileStream fs = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fs, dto, _options).ConfigureAwait(false);
    }

    /// <summary>Loads an action map from the specified JSON file path. The caller must apply the result on the main thread via <see cref="InputManager.LoadMap"/>.</summary>
    /// <param name="filePath">Source file path.</param>
    /// <returns>The deserialized <see cref="InputActionMap"/>.</returns>
    public async Task<InputActionMap> Load(string filePath)
    {
        await using FileStream fs = File.OpenRead(filePath);
        ActionMapDto? dto = await JsonSerializer.DeserializeAsync<ActionMapDto>(fs, _options).ConfigureAwait(false);

        InputActionMap map = new();
        if (dto is null) return map;

        ActionDto[] actionDtos = dto.Actions;
        for (int i = 0; i < actionDtos.Length; i++)
        {
            ActionDto actionDto = actionDtos[i];
            InputBinding[] bindings = actionDto.Bindings;

            int keyCount = 0, padCount = 0, mouseCount = 0;
            for (int j = 0; j < bindings.Length; j++)
            {
                switch (bindings[j].DeviceType)
                {
                    case DeviceType.Keyboard: keyCount++; break;
                    case DeviceType.Gamepad:  padCount++;  break;
                    case DeviceType.Mouse:    mouseCount++; break;
                }
            }

            Keys[] keys   = new Keys[keyCount];
            Buttons[] pads  = new Buttons[padCount];
            MouseButton[] mouse = new MouseButton[mouseCount];
            int ki = 0, pi = 0, mi = 0;

            for (int j = 0; j < bindings.Length; j++)
            {
                InputBinding b = bindings[j];
                switch (b.DeviceType)
                {
                    case DeviceType.Keyboard: keys[ki++]   = (Keys)b.Code;        break;
                    case DeviceType.Gamepad:  pads[pi++]   = (Buttons)b.Code;     break;
                    case DeviceType.Mouse:    mouse[mi++]  = (MouseButton)b.Code; break;
                }
            }

            map.Register(new InputAction(actionDto.Name, keys, pads, mouse));
        }

        return map;
    }

    private sealed class ActionMapDto
    {
        public ActionDto[] Actions { get; set; } = [];
    }

    private sealed class ActionDto
    {
        public string Name { get; set; } = string.Empty;
        public InputBinding[] Bindings { get; set; } = [];
    }
}
