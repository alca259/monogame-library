using System.Text.Json;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Maui.Drawers.BuiltIn;

// Usa sobrecarga de string porque PointLight2DBehaviour puede no estar en la versión del paquete NuGet instalado.
[CustomBehaviourEditor("Alca.MonoGame.Kernel.Lighting.PointLight2DBehaviour")]
internal sealed class PointLight2DEditor : BehaviourEditor
{
    public override IReadOnlyList<string> RadiusPreviewProperties => ["Range"];

    public override View BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        VerticalStackLayout layout = new() { Spacing = 0 };

        // Color
        if (behaviour.Properties.TryGetValue("Color", out JsonElement colorEl)
            && PropertyControlHelper.IsColorValue(colorEl))
        {
            layout.Children.Add(BuildColorField("Color", colorEl,
                nv => SetProperty(behaviour, "Color", nv)));
        }

        // Intensity [0, 1]
        if (behaviour.Properties.TryGetValue("Intensity", out JsonElement intensityEl)
            && intensityEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildSliderField("Intensity", intensityEl.GetDouble(), 0.0, 1.0,
                v => SetProperty(behaviour, "Intensity", JsonSerializer.SerializeToElement((float)v))));
        }

        // Range — slider + radius preview en el viewport
        if (behaviour.Properties.TryGetValue("Range", out JsonElement rangeEl)
            && rangeEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildSliderField("Range", rangeEl.GetDouble(), 0.0, 2000.0,
                v => SetProperty(behaviour, "Range", JsonSerializer.SerializeToElement((float)v))));
        }

        // FalloffExponent [0, 10]
        if (behaviour.Properties.TryGetValue("FalloffExponent", out JsonElement fallEl)
            && fallEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildSliderField("Falloff Exp.", fallEl.GetDouble(), 0.1, 10.0,
                v => SetProperty(behaviour, "FalloffExponent", JsonSerializer.SerializeToElement((float)v))));
        }

        return layout;
    }
}
