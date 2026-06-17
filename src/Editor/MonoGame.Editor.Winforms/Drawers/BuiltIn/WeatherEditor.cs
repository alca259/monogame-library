using System.Text.Json;
using System.Windows.Forms;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Winforms.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Weather.WeatherBehaviour")]
internal sealed class WeatherEditor : BehaviourEditor
{
    public override Control BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        List<Control> rows = [];

        if (behaviour.Properties.TryGetValue("ReceivesWind", out JsonElement windEl)
            && windEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            rows.Add(BuildBoolField("Receives Wind", windEl.GetBoolean(),
                v => SetProperty(behaviour, "ReceivesWind", JsonSerializer.SerializeToElement(v))));
        }

        if (behaviour.Properties.TryGetValue("WindForceMultiplier", out JsonElement multEl)
            && multEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildSliderField("Wind Multiplier", multEl.GetDouble(), 0.0, 5.0,
                v => SetProperty(behaviour, "WindForceMultiplier", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("ReceivesLightningImpulse", out JsonElement lightEl)
            && lightEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            rows.Add(BuildBoolField("Receives Lightning", lightEl.GetBoolean(),
                v => SetProperty(behaviour, "ReceivesLightningImpulse", JsonSerializer.SerializeToElement(v))));
        }

        return BuildCard(rows);
    }
}
