using System.Text.Json;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Maui.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Weather.WeatherBehaviour")]
internal sealed class WeatherEditor : BehaviourEditor
{
    public override View BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        VerticalStackLayout layout = new() { Spacing = 0 };

        if (behaviour.Properties.TryGetValue("ReceivesWind", out JsonElement windEl)
            && windEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            layout.Children.Add(BuildBoolField("Receives Wind", windEl.GetBoolean(),
                v => SetProperty(behaviour, "ReceivesWind", JsonSerializer.SerializeToElement(v))));
        }

        if (behaviour.Properties.TryGetValue("WindForceMultiplier", out JsonElement multEl)
            && multEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildSliderField("Wind Multiplier", multEl.GetDouble(), 0.0, 5.0,
                v => SetProperty(behaviour, "WindForceMultiplier", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("ReceivesLightningImpulse", out JsonElement lightEl)
            && lightEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            layout.Children.Add(BuildBoolField("Receives Lightning", lightEl.GetBoolean(),
                v => SetProperty(behaviour, "ReceivesLightningImpulse", JsonSerializer.SerializeToElement(v))));
        }

        return layout;
    }
}
