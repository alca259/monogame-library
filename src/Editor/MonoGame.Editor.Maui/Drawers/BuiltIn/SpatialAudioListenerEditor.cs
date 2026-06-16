using System.Text.Json;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Maui.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Audio.Spatial.SpatialAudioListenerBehaviour")]
internal sealed class SpatialAudioListenerEditor : BehaviourEditor
{
    public override View BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        VerticalStackLayout layout = new() { Spacing = 0 };

        if (behaviour.Properties.TryGetValue("IsMain", out JsonElement el)
            && el.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            layout.Children.Add(BuildBoolField("Is Main", el.GetBoolean(),
                v => SetProperty(behaviour, "IsMain", JsonSerializer.SerializeToElement(v))));
        }

        return layout;
    }
}
