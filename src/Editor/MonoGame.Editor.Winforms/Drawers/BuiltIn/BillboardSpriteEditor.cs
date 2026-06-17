using System.Text.Json;
using System.Windows.Forms;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Winforms.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Graphics.Sprites.BillboardSpriteBehaviour")]
internal sealed class BillboardSpriteEditor : BehaviourEditor
{
    public override Control BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        List<Control> rows = [];

        if (behaviour.Properties.TryGetValue("Color", out JsonElement colorEl)
            && PropertyControlHelper.IsColorValue(colorEl))
        {
            rows.Add(BuildColorField("Color", colorEl,
                nv => SetProperty(behaviour, "Color", nv)));
        }

        if (behaviour.Properties.TryGetValue("LayerDepth", out JsonElement depthEl)
            && depthEl.ValueKind == JsonValueKind.Number)
        {
            rows.Add(BuildSliderField("Layer Depth", depthEl.GetDouble(), 0.0, 1.0,
                v => SetProperty(behaviour, "LayerDepth", JsonSerializer.SerializeToElement((float)v))));
        }

        return BuildCard(rows);
    }
}
