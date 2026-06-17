using System.Text.Json;
using System.Windows.Forms;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Winforms.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Audio.Spatial.SpatialAudioSourceBehaviour")]
internal sealed class SpatialAudioSourceEditor : BehaviourEditor
{
    public override Control BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        List<Control> rows = [];

        if (behaviour.Properties.TryGetValue("Volume", out JsonElement volEl)
            && volEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildSliderField("Volume", volEl.GetDouble(), 0.0, 1.0,
                v => SetProperty(behaviour, "Volume", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("Pitch", out JsonElement pitchEl)
            && pitchEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildSliderField("Pitch", pitchEl.GetDouble(), -1.0, 1.0,
                v => SetProperty(behaviour, "Pitch", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("Loop", out JsonElement loopEl)
            && loopEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            rows.Add(BuildBoolField("Loop", loopEl.GetBoolean(),
                v => SetProperty(behaviour, "Loop", JsonSerializer.SerializeToElement(v))));
        }

        if (behaviour.Properties.TryGetValue("PlayOnAwake", out JsonElement poaEl)
            && poaEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            rows.Add(BuildBoolField("Play On Awake", poaEl.GetBoolean(),
                v => SetProperty(behaviour, "PlayOnAwake", JsonSerializer.SerializeToElement(v))));
        }

        return BuildCard(rows);
    }
}
