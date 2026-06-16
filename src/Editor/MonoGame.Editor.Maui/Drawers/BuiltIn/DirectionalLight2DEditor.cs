using System.Text.Json;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Maui.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Lighting.DirectionalLight2DBehaviour")]
internal sealed class DirectionalLight2DEditor : BehaviourEditor
{
    public override View BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        VerticalStackLayout layout = new() { Spacing = 0 };

        // Propiedades heredadas de LightBehaviour
        if (behaviour.Properties.TryGetValue("Color", out JsonElement colorEl)
            && PropertyControlHelper.IsColorValue(colorEl))
        {
            layout.Children.Add(BuildColorField("Color", colorEl,
                nv => SetProperty(behaviour, "Color", nv)));
        }

        if (behaviour.Properties.TryGetValue("Intensity", out JsonElement intensityEl)
            && intensityEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildSliderField("Intensity", intensityEl.GetDouble(), 0.0, 1.0,
                v => SetProperty(behaviour, "Intensity", JsonSerializer.SerializeToElement((float)v))));
        }

        // Direction (Vector2)
        if (behaviour.Properties.TryGetValue("Direction", out JsonElement dirEl)
            && dirEl.ValueKind == JsonValueKind.Object)
        {
            layout.Children.Add(BuildVector2Field("Direction", dirEl,
                v => SetProperty(behaviour, "Direction", SerializeVector2(v,
                    PropertyControlHelper.GetVector2(behaviour.Properties.TryGetValue("Direction", out var d) ? d : dirEl).Y)),
                v => SetProperty(behaviour, "Direction", SerializeVector2(
                    PropertyControlHelper.GetVector2(behaviour.Properties.TryGetValue("Direction", out var d) ? d : dirEl).X, v))));
        }

        return layout;
    }
}
