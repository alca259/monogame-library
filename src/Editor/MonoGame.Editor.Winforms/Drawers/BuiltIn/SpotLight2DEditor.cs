using System.Text.Json;
using System.Windows.Forms;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Winforms.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Lighting.SpotLight2DBehaviour")]
internal sealed class SpotLight2DEditor : BehaviourEditor
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

        if (behaviour.Properties.TryGetValue("InnerAngle", out JsonElement innerEl)
            && innerEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildSliderField("Inner Angle°", innerEl.GetDouble(), 0.0, 180.0,
                v => SetProperty(behaviour, "InnerAngle", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("OuterAngle", out JsonElement outerEl)
            && outerEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildSliderField("Outer Angle°", outerEl.GetDouble(), 0.0, 180.0,
                v => SetProperty(behaviour, "OuterAngle", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("Direction", out JsonElement dirEl)
            && dirEl.ValueKind == JsonValueKind.Object)
        {
            rows.Add(BuildVector2Field("Direction", dirEl,
                v => SetProperty(behaviour, "Direction", SerializeVector2(v,
                    PropertyControlHelper.GetVector2(behaviour.Properties.TryGetValue("Direction", out var d) ? d : dirEl).Y)),
                v => SetProperty(behaviour, "Direction", SerializeVector2(
                    PropertyControlHelper.GetVector2(behaviour.Properties.TryGetValue("Direction", out var d) ? d : dirEl).X, v))));
        }

        return BuildCard(rows);
    }
}
