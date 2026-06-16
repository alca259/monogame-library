using System.Text.Json;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Maui.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Graphics.Sprites.BillboardSpriteBehaviour")]
internal sealed class BillboardSpriteEditor : BehaviourEditor
{
    public override View BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        VerticalStackLayout layout = new() { Spacing = 0 };

        if (behaviour.Properties.TryGetValue("Color", out JsonElement colorEl)
            && PropertyControlHelper.IsColorValue(colorEl))
        {
            layout.Children.Add(BuildColorField("Color", colorEl,
                nv => SetProperty(behaviour, "Color", nv)));
        }

        if (behaviour.Properties.TryGetValue("LayerDepth", out JsonElement depthEl)
            && depthEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildSliderField("Layer Depth", depthEl.GetDouble(), 0.0, 1.0,
                v => SetProperty(behaviour, "LayerDepth", JsonSerializer.SerializeToElement((float)v))));
        }

        return layout;
    }
}
