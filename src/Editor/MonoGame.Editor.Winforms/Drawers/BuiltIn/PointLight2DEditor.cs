using System.Text.Json;
using System.Windows.Forms;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Winforms.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Lighting.PointLight2DBehaviour")]
internal sealed class PointLight2DEditor : BehaviourEditor
{
    public override IReadOnlyList<string> RadiusPreviewProperties => ["Range"];

    public override Control BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        List<Control> rows = [];

        if (behaviour.Properties.TryGetValue("Color", out JsonElement colorEl)
            && PropertyControlHelper.IsColorValue(colorEl))
        {
            rows.Add(BuildColorField("Color", colorEl,
                nv => SetProperty(behaviour, "Color", nv)));
        }

        if (behaviour.Properties.TryGetValue("Intensity", out JsonElement intensityEl)
            && intensityEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildSliderField("Intensity", intensityEl.GetDouble(), 0.0, 1.0,
                v => SetProperty(behaviour, "Intensity", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("Range", out JsonElement rangeEl)
            && rangeEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildSliderField("Range", rangeEl.GetDouble(), 0.0, 2000.0,
                v => SetProperty(behaviour, "Range", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("FalloffExponent", out JsonElement fallEl)
            && fallEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildSliderField("Falloff Exp.", fallEl.GetDouble(), 0.1, 10.0,
                v => SetProperty(behaviour, "FalloffExponent", JsonSerializer.SerializeToElement((float)v))));
        }

        return BuildCard(rows);
    }
}
