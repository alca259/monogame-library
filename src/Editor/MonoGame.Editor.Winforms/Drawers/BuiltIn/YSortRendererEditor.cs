using System.Text.Json;
using System.Windows.Forms;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Winforms.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Graphics.Sprites.YSortRendererBehaviour")]
internal sealed class YSortRendererEditor : BehaviourEditor
{
    public override Control BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        List<Control> rows = [];

        if (behaviour.Properties.TryGetValue("SpritePath", out JsonElement pathEl))
        {
            string path = pathEl.ValueKind == JsonValueKind.String ? pathEl.GetString() ?? "" : "";
            rows.Add(BuildFilePickerField("Sprite Path", path,
                [".png", ".jpg", ".jpeg", ".xnb"],
                v => SetProperty(behaviour, "SpritePath", JsonSerializer.SerializeToElement(v))));
        }

        if (behaviour.Properties.TryGetValue("Color", out JsonElement colorEl)
            && PropertyControlHelper.IsColorValue(colorEl))
        {
            rows.Add(BuildColorField("Color", colorEl,
                nv => SetProperty(behaviour, "Color", nv)));
        }

        if (behaviour.Properties.TryGetValue("WorldHeight", out JsonElement heightEl)
            && heightEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildNumberField("World Height", heightEl.GetDouble(),
                v => SetProperty(behaviour, "WorldHeight", JsonSerializer.SerializeToElement((float)v))));
        }

        if (behaviour.Properties.TryGetValue("YOffset", out JsonElement yOffsetEl)
            && yOffsetEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildNumberField("Y Offset", yOffsetEl.GetDouble(),
                v => SetProperty(behaviour, "YOffset", JsonSerializer.SerializeToElement((int)v))));
        }

        return BuildCard(rows);
    }
}
