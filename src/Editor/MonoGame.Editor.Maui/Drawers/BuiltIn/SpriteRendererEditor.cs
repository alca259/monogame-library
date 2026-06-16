using System.Text.Json;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Maui.Drawers.BuiltIn;

// Usa sobrecarga de string para compatibilidad con distintas versiones del paquete NuGet.
[CustomBehaviourEditor("Alca.MonoGame.Kernel.ECS.SpriteRendererBehaviour")]
internal sealed class SpriteRendererEditor : BehaviourEditor
{
    public override View BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        VerticalStackLayout layout = new() { Spacing = 0 };

        // SpritePath — file picker filtrado a imágenes/content
        if (behaviour.Properties.TryGetValue("SpritePath", out JsonElement pathEl))
        {
            string path = pathEl.ValueKind == JsonValueKind.String ? pathEl.GetString() ?? "" : "";
            layout.Children.Add(BuildFilePickerField("Sprite Path", path,
                [".png", ".jpg", ".jpeg", ".xnb"],
                v => SetProperty(behaviour, "SpritePath", JsonSerializer.SerializeToElement(v))));
        }

        // Color — color picker
        if (behaviour.Properties.TryGetValue("Color", out JsonElement colorEl)
            && PropertyControlHelper.IsColorValue(colorEl))
        {
            layout.Children.Add(BuildColorField("Color", colorEl,
                nv => SetProperty(behaviour, "Color", nv)));
        }

        // LayerDepth — slider [0, 1]
        if (behaviour.Properties.TryGetValue("LayerDepth", out JsonElement depthEl)
            && depthEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildSliderField("Layer Depth", depthEl.GetDouble(), 0.0, 1.0,
                v => SetProperty(behaviour, "LayerDepth", JsonSerializer.SerializeToElement((float)v))));
        }

        return layout;
    }
}
