using System.Text.Json;
using System.Windows.Forms;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Winforms.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Navigation.Steering.SteeringControllerBehaviour")]
internal sealed class SteeringControllerEditor : BehaviourEditor
{
    public override Control BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        List<Control> rows = [];

        if (behaviour.Properties.TryGetValue("MaxResultSpeed", out JsonElement speedEl)
            && speedEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildSliderField("Max Speed", speedEl.GetDouble(), 0.0, 2000.0,
                v => SetProperty(behaviour, "MaxResultSpeed", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("ApplyToTransform", out JsonElement applyEl)
            && applyEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            rows.Add(BuildBoolField("Apply To Transform", applyEl.GetBoolean(),
                v => SetProperty(behaviour, "ApplyToTransform", JsonSerializer.SerializeToElement(v))));
        }

        return BuildCard(rows);
    }
}
