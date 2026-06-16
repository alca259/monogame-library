using System.Text.Json;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Maui.Drawers.BuiltIn;

// Usa sobrecarga de string porque AudioZoneBehaviour puede no estar en la versión del paquete NuGet instalado.
[CustomBehaviourEditor("Alca.MonoGame.Kernel.Audio.Ambient.AudioZoneBehaviour")]
internal sealed class AudioZoneEditor : BehaviourEditor
{
    public override IReadOnlyList<string> RadiusPreviewProperties => ["Radius"];

    public override View BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        VerticalStackLayout layout = new() { Spacing = 0 };

        // Radius — slider + radius circle en viewport (comportamiento ya implementado en DrawBehaviourGizmos)
        if (behaviour.Properties.TryGetValue("Radius", out JsonElement radiusEl)
            && radiusEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildSliderField("Radius", radiusEl.GetDouble(), 0.0, 1000.0,
                v => SetProperty(behaviour, "Radius", JsonSerializer.SerializeToElement((float)v))));
        }

        // FadeInTime [0, 30]
        if (behaviour.Properties.TryGetValue("FadeInTime", out JsonElement fadeInEl)
            && fadeInEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildSliderField("Fade In (s)", fadeInEl.GetDouble(), 0.0, 30.0,
                v => SetProperty(behaviour, "FadeInTime", JsonSerializer.SerializeToElement((float)v))));
        }

        // FadeOutTime [0, 30]
        if (behaviour.Properties.TryGetValue("FadeOutTime", out JsonElement fadeOutEl)
            && fadeOutEl.ValueKind == JsonValueKind.Number)
        {
            layout.Children.Add(BuildSliderField("Fade Out (s)", fadeOutEl.GetDouble(), 0.0, 30.0,
                v => SetProperty(behaviour, "FadeOutTime", JsonSerializer.SerializeToElement((float)v))));
        }

        return layout;
    }
}
