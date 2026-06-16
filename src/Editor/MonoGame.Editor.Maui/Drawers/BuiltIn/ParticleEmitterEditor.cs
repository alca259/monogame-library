using System.Text.Json;
using MonoGame.Editor.Core.Attributes;

namespace MonoGame.Editor.Maui.Drawers.BuiltIn;

// Usa sobrecarga de string para compatibilidad con distintas versiones del paquete NuGet.
[CustomBehaviourEditor("Alca.MonoGame.Kernel.Graphics.Particles.ParticleEmitterBehaviour")]
internal sealed class ParticleEmitterEditor : BehaviourEditor
{
    public override View BuildInspector(EditorBehaviour behaviour, EditorGameObject owner)
    {
        VerticalStackLayout layout = new() { Spacing = 0 };

        // UseEntityPosition — checkbox
        if (behaviour.Properties.TryGetValue("UseEntityPosition", out JsonElement useEntityEl)
            && useEntityEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            layout.Children.Add(BuildBoolField("Follow Entity", useEntityEl.GetBoolean(),
                v => SetProperty(behaviour, "UseEntityPosition", JsonSerializer.SerializeToElement(v))));
        }

        // Offset X / Y — campos numéricos
        if (behaviour.Properties.TryGetValue("Offset", out JsonElement offsetEl)
            && offsetEl.ValueKind == JsonValueKind.Object)
        {
            double ox = offsetEl.TryGetProperty("X", out JsonElement oxEl) ? oxEl.GetDouble() : 0.0;
            double oy = offsetEl.TryGetProperty("Y", out JsonElement oyEl) ? oyEl.GetDouble() : 0.0;

            layout.Children.Add(BuildHeaderSeparator("Offset"));
            layout.Children.Add(BuildNumberField("X", ox, v =>
            {
                double cy = behaviour.Properties.TryGetValue("Offset", out JsonElement cur)
                    && cur.TryGetProperty("Y", out JsonElement yp) ? yp.GetDouble() : 0.0;
                SetProperty(behaviour, "Offset",
                    JsonSerializer.SerializeToElement(new { X = (float)v, Y = (float)cy }));
            }));
            layout.Children.Add(BuildNumberField("Y", oy, v =>
            {
                double cx = behaviour.Properties.TryGetValue("Offset", out JsonElement cur)
                    && cur.TryGetProperty("X", out JsonElement xp) ? xp.GetDouble() : 0.0;
                SetProperty(behaviour, "Offset",
                    JsonSerializer.SerializeToElement(new { X = (float)cx, Y = (float)v }));
            }));
        }

        return layout;
    }
}
