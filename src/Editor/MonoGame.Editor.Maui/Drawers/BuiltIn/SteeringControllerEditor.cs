using System.Text.Json;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Maui.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Navigation.Steering.SteeringControllerBehaviour")]
internal sealed class SteeringControllerEditor : BehaviourEditor
{
    public override View BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        VerticalStackLayout layout = new() { Spacing = 0 };

        if (behaviour.Properties.TryGetValue("MaxResultSpeed", out JsonElement speedEl)
            && speedEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildSliderField("Max Speed", speedEl.GetDouble(), 0.0, 2000.0,
                v => SetProperty(behaviour, "MaxResultSpeed", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("ApplyToTransform", out JsonElement applyEl)
            && applyEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            layout.Children.Add(BuildBoolField("Apply To Transform", applyEl.GetBoolean(),
                v => SetProperty(behaviour, "ApplyToTransform", JsonSerializer.SerializeToElement(v))));
        }

        return layout;
    }
}
