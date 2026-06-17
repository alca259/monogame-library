using System.Text.Json;
using System.Windows.Forms;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Winforms.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Audio.Ambient.AudioZoneBehaviour")]
internal sealed class AudioZoneEditor : BehaviourEditor
{
    public override IReadOnlyList<string> RadiusPreviewProperties => ["Radius"];

    public override Control BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        List<Control> rows = [];

        if (behaviour.Properties.TryGetValue("Radius", out JsonElement radiusEl)
            && radiusEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildSliderField("Radius", radiusEl.GetDouble(), 0.0, 1000.0,
                v => SetProperty(behaviour, "Radius", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("FadeInTime", out JsonElement fadeInEl)
            && fadeInEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildSliderField("Fade In (s)", fadeInEl.GetDouble(), 0.0, 30.0,
                v => SetProperty(behaviour, "FadeInTime", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("FadeOutTime", out JsonElement fadeOutEl)
            && fadeOutEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildSliderField("Fade Out (s)", fadeOutEl.GetDouble(), 0.0, 30.0,
                v => SetProperty(behaviour, "FadeOutTime", JsonSerializer.SerializeToElement((float)v))));
        }

        return BuildCard(rows);
    }
}
