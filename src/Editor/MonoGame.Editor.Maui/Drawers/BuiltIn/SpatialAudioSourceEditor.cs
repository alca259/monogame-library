using System.Text.Json;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Maui.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Audio.Spatial.SpatialAudioSourceBehaviour")]
internal sealed class SpatialAudioSourceEditor : BehaviourEditor
{
    public override View BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        VerticalStackLayout layout = new() { Spacing = 0 };

        // Volume [0, 1]
        if (behaviour.Properties.TryGetValue("Volume", out JsonElement volEl)
            && volEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildSliderField("Volume", volEl.GetDouble(), 0.0, 1.0,
                v => SetProperty(behaviour, "Volume", JsonSerializer.SerializeToElement((float)v))));
        }

        // Pitch [-1, 1]
        if (behaviour.Properties.TryGetValue("Pitch", out JsonElement pitchEl)
            && pitchEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildSliderField("Pitch", pitchEl.GetDouble(), -1.0, 1.0,
                v => SetProperty(behaviour, "Pitch", JsonSerializer.SerializeToElement((float)v))));
        }

        // Loop
        if (behaviour.Properties.TryGetValue("Loop", out JsonElement loopEl)
            && loopEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            layout.Children.Add(BuildBoolField("Loop", loopEl.GetBoolean(),
                v => SetProperty(behaviour, "Loop", JsonSerializer.SerializeToElement(v))));
        }

        // PlayOnAwake
        if (behaviour.Properties.TryGetValue("PlayOnAwake", out JsonElement poaEl)
            && poaEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            layout.Children.Add(BuildBoolField("Play On Awake", poaEl.GetBoolean(),
                v => SetProperty(behaviour, "PlayOnAwake", JsonSerializer.SerializeToElement(v))));
        }

        return layout;
    }
}
