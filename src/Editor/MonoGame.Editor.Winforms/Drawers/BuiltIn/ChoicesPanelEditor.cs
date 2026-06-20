using System.Text.Json;
using System.Windows.Forms;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Winforms.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Dialogue.ChoicesPanelBehaviour")]
internal sealed class ChoicesPanelEditor : BehaviourEditor
{
    public override Control BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        List<Control> rows = [];

        if (behaviour.Properties.TryGetValue("Position", out JsonElement posEl)
            && posEl.ValueKind == JsonValueKind.Object)
        {
            rows.Add(BuildVector2Field("Position", posEl,
                v => SetProperty(behaviour, "Position", SerializeVector2(v,
                    PropertyControlHelper.GetVector2(behaviour.Properties.TryGetValue("Position", out var p) ? p : posEl).Y)),
                v => SetProperty(behaviour, "Position", SerializeVector2(
                    PropertyControlHelper.GetVector2(behaviour.Properties.TryGetValue("Position", out var p) ? p : posEl).X, v))));
        }

        rows.Add(BuildHeaderSeparator("Button"));

        if (behaviour.Properties.TryGetValue("ButtonWidth", out JsonElement bwEl)
            && bwEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildSliderField("Width", bwEl.GetDouble(), 50.0, 800.0,
                v => SetProperty(behaviour, "ButtonWidth", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("ButtonHeight", out JsonElement bhEl)
            && bhEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildSliderField("Height", bhEl.GetDouble(), 20.0, 200.0,
                v => SetProperty(behaviour, "ButtonHeight", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("ButtonSpacing", out JsonElement bsEl)
            && bsEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildSliderField("Spacing", bsEl.GetDouble(), 0.0, 50.0,
                v => SetProperty(behaviour, "ButtonSpacing", JsonSerializer.SerializeToElement((float)v))));
        }

        rows.Add(BuildHeaderSeparator("Colors"));

        if (behaviour.Properties.TryGetValue("NormalColor", out JsonElement normalEl)
            && PropertyControlHelper.IsColorValue(normalEl))
        {
            rows.Add(BuildColorField("Normal", normalEl,
                nv => SetProperty(behaviour, "NormalColor", nv)));
        }

        if (behaviour.Properties.TryGetValue("HoverColor", out JsonElement hoverEl)
            && PropertyControlHelper.IsColorValue(hoverEl))
        {
            rows.Add(BuildColorField("Hover", hoverEl,
                nv => SetProperty(behaviour, "HoverColor", nv)));
        }

        if (behaviour.Properties.TryGetValue("TextColor", out JsonElement textEl)
            && PropertyControlHelper.IsColorValue(textEl))
        {
            rows.Add(BuildColorField("Text", textEl,
                nv => SetProperty(behaviour, "TextColor", nv)));
        }

        return BuildCard(rows);
    }
}
