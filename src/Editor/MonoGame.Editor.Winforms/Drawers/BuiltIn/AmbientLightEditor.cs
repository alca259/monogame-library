using System.Text.Json;
using System.Windows.Forms;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Winforms.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Lighting.AmbientLightBehaviour")]
internal sealed class AmbientLightEditor : BehaviourEditor
{
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

        return BuildCard(rows);
    }
}
