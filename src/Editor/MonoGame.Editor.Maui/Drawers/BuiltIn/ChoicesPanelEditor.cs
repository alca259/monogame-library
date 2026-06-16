using System.Text.Json;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Maui.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Dialogue.ChoicesPanelBehaviour")]
internal sealed class ChoicesPanelEditor : BehaviourEditor
{
    public override View BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        VerticalStackLayout layout = new() { Spacing = 0 };

        // Position (Vector2)
        if (behaviour.Properties.TryGetValue("Position", out JsonElement posEl)
            && posEl.ValueKind == JsonValueKind.Object)
        {
            layout.Children.Add(BuildVector2Field("Position", posEl,
                v => SetProperty(behaviour, "Position", SerializeVector2(v,
                    PropertyControlHelper.GetVector2(behaviour.Properties.TryGetValue("Position", out var p) ? p : posEl).Y)),
                v => SetProperty(behaviour, "Position", SerializeVector2(
                    PropertyControlHelper.GetVector2(behaviour.Properties.TryGetValue("Position", out var p) ? p : posEl).X, v))));
        }

        // Button dimensions
        layout.Children.Add(BuildHeaderSeparator("Button"));

        if (behaviour.Properties.TryGetValue("ButtonWidth", out JsonElement bwEl)
            && bwEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildSliderField("Width", bwEl.GetDouble(), 50.0, 800.0,
                v => SetProperty(behaviour, "ButtonWidth", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("ButtonHeight", out JsonElement bhEl)
            && bhEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildSliderField("Height", bhEl.GetDouble(), 20.0, 200.0,
                v => SetProperty(behaviour, "ButtonHeight", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("ButtonSpacing", out JsonElement bsEl)
            && bsEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildSliderField("Spacing", bsEl.GetDouble(), 0.0, 50.0,
                v => SetProperty(behaviour, "ButtonSpacing", JsonSerializer.SerializeToElement((float)v))));
        }

        // Colors
        layout.Children.Add(BuildHeaderSeparator("Colors"));

        if (behaviour.Properties.TryGetValue("NormalColor", out JsonElement normalEl)
            && PropertyControlHelper.IsColorValue(normalEl))
        {
            layout.Children.Add(BuildColorField("Normal", normalEl,
                nv => SetProperty(behaviour, "NormalColor", nv)));
        }

        if (behaviour.Properties.TryGetValue("HoverColor", out JsonElement hoverEl)
            && PropertyControlHelper.IsColorValue(hoverEl))
        {
            layout.Children.Add(BuildColorField("Hover", hoverEl,
                nv => SetProperty(behaviour, "HoverColor", nv)));
        }

        if (behaviour.Properties.TryGetValue("TextColor", out JsonElement textEl)
            && PropertyControlHelper.IsColorValue(textEl))
        {
            layout.Children.Add(BuildColorField("Text", textEl,
                nv => SetProperty(behaviour, "TextColor", nv)));
        }

        return layout;
    }
}
