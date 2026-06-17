using System.Text.Json;
using System.Windows.Forms;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Winforms.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Dialogue.DialogueBoxBehaviour")]
internal sealed class DialogueBoxEditor : BehaviourEditor
{
    public override Control BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        List<Control> rows = [];

        if (behaviour.Properties.TryGetValue("Visible", out JsonElement visibleEl)
            && visibleEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            rows.Add(BuildBoolField("Visible", visibleEl.GetBoolean(),
                v => SetProperty(behaviour, "Visible", JsonSerializer.SerializeToElement(v))));
        }

        if (behaviour.Properties.TryGetValue("Position", out JsonElement posEl)
            && posEl.ValueKind == JsonValueKind.Object)
        {
            rows.Add(BuildVector2Field("Position", posEl,
                v => SetProperty(behaviour, "Position", SerializeVector2(v,
                    PropertyControlHelper.GetVector2(behaviour.Properties.TryGetValue("Position", out var p) ? p : posEl).Y)),
                v => SetProperty(behaviour, "Position", SerializeVector2(
                    PropertyControlHelper.GetVector2(behaviour.Properties.TryGetValue("Position", out var p) ? p : posEl).X, v))));
        }

        if (behaviour.Properties.TryGetValue("Size", out JsonElement sizeEl)
            && sizeEl.ValueKind == JsonValueKind.Object)
        {
            rows.Add(BuildVector2Field("Size", sizeEl,
                v => SetProperty(behaviour, "Size", SerializeVector2(v,
                    PropertyControlHelper.GetVector2(behaviour.Properties.TryGetValue("Size", out var s) ? s : sizeEl).Y)),
                v => SetProperty(behaviour, "Size", SerializeVector2(
                    PropertyControlHelper.GetVector2(behaviour.Properties.TryGetValue("Size", out var s) ? s : sizeEl).X, v))));
        }

        if (behaviour.Properties.TryGetValue("Padding", out JsonElement paddingEl)
            && paddingEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildSliderField("Padding", paddingEl.GetDouble(), 0.0, 50.0,
                v => SetProperty(behaviour, "Padding", JsonSerializer.SerializeToElement((int)v))));
        }

        rows.Add(BuildHeaderSeparator("Colors"));

        if (behaviour.Properties.TryGetValue("BackgroundColor", out JsonElement bgEl)
            && PropertyControlHelper.IsColorValue(bgEl))
        {
            rows.Add(BuildColorField("Background", bgEl,
                nv => SetProperty(behaviour, "BackgroundColor", nv)));
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
