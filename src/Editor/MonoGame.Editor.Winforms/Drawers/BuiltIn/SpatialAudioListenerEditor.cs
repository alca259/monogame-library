using System.Text.Json;
using System.Windows.Forms;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Winforms.Drawers.BuiltIn;

[CustomBehaviourEditor("Alca.MonoGame.Kernel.Audio.Spatial.SpatialAudioListenerBehaviour")]
internal sealed class SpatialAudioListenerEditor : BehaviourEditor
{
    public override Control BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        List<Control> rows = [];

        if (behaviour.Properties.TryGetValue("IsMain", out JsonElement el)
            && el.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            rows.Add(BuildBoolField("Is Main", el.GetBoolean(),
                v => SetProperty(behaviour, "IsMain", JsonSerializer.SerializeToElement(v))));
        }

        return BuildCard(rows);
    }
}
