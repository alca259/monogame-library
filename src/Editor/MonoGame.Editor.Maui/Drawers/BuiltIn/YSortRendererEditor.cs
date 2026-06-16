using System.Text.Json;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Maui.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Graphics.Sprites.YSortRendererBehaviour")]
internal sealed class YSortRendererEditor : BehaviourEditor
{
    public override View BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        VerticalStackLayout layout = new() { Spacing = 0 };

        if (behaviour.Properties.TryGetValue("SpritePath", out JsonElement pathEl))
        {
            string path = pathEl.ValueKind == JsonValueKind.String ? pathEl.GetString() ?? "" : "";
            layout.Children.Add(BuildFilePickerField("Sprite Path", path,
                [".png", ".jpg", ".jpeg", ".xnb"],
                v => SetProperty(behaviour, "SpritePath", JsonSerializer.SerializeToElement(v))));
        }

        if (behaviour.Properties.TryGetValue("Color", out JsonElement colorEl)
            && PropertyControlHelper.IsColorValue(colorEl))
        {
            layout.Children.Add(BuildColorField("Color", colorEl,
                nv => SetProperty(behaviour, "Color", nv)));
        }

        if (behaviour.Properties.TryGetValue("WorldHeight", out JsonElement heightEl)
            && heightEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildNumberField("World Height", heightEl.GetDouble(),
                v => SetProperty(behaviour, "WorldHeight", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("YOffset", out JsonElement yOffsetEl)
            && yOffsetEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildNumberField("Y Offset", yOffsetEl.GetDouble(),
                v => SetProperty(behaviour, "YOffset", JsonSerializer.SerializeToElement((int)v))));
        }

        return layout;
    }
}
