using Alca.MonoGame.Kernel.Lighting.GPU;

namespace Alca.MonoGame.Kernel.Lighting;

/// <summary>
/// Singleton service that aggregates all registered lights and resolves illumination
/// at any world position. Assign to <see cref="ECS.GameWorld.LightingWorld"/> to enable
/// automatic registration of <see cref="LightBehaviour"/> components.
/// </summary>
public sealed class LightingWorld
{
    private readonly List<LightBehaviour> _lights = new(32);

    /// <summary>Gets or sets the ambient fallback color used when no lights contribute. Default is <see cref="Color.Black"/>.</summary>
    public Color AmbientColor { get; set; } = Color.Black;

    /// <summary>Gets the number of currently registered lights.</summary>
    public int LightCount => _lights.Count;

    // ── Registration ──────────────────────────────────────────────────────────

    /// <summary>Registers <paramref name="light"/> so it participates in <see cref="Resolve"/> queries. No-op if already registered.</summary>
    public void Register(LightBehaviour light)
    {
        if (!_lights.Contains(light))
            _lights.Add(light);
    }

    /// <summary>Unregisters <paramref name="light"/> from lighting calculations.</summary>
    public void Unregister(LightBehaviour light) => _lights.Remove(light);

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the accumulated illumination color at <paramref name="worldPosition"/> for the given <paramref name="layer"/>.
    /// Starts from <see cref="AmbientColor"/> and blends each contributing light in registration order.
    /// </summary>
    public Color Resolve(Vector2 worldPosition, LightingLayer layer)
    {
        var accumulation = new LightContribution(AmbientColor);

        for (int i = 0; i < _lights.Count; i++)
        {
            var light = _lights[i];
            if (!light.IsContributing) continue;
            if (light.LightingLayer != layer) continue;
            light.Contribute(ref accumulation, worldPosition);
        }

        return accumulation.Accumulated;
    }

    /// <summary>
    /// Fills <paramref name="results"/> with all contributing lights whose entity position is within
    /// <paramref name="radius"/> of <paramref name="position"/> on the given <paramref name="layer"/>.
    /// No allocations.
    /// </summary>
    public void GetLightsInRange(Vector2 position, float radius, LightingLayer layer, List<LightBehaviour> results)
    {
        for (int i = 0; i < _lights.Count; i++)
        {
            var light = _lights[i];
            if (!light.IsContributing) continue;
            if (light.LightingLayer != layer) continue;

            float dist = Vector2.Distance(light.Entity.Transform.Position2d, position);
            if (dist <= radius)
                results.Add(light);
        }
    }

    /// <summary>
    /// Fills <paramref name="buffer"/> with <see cref="LightShaderData"/> for all contributing lights
    /// on the specified <paramref name="layer"/>, up to <paramref name="maxLights"/> entries.
    /// No allocations — call every frame safely.
    /// </summary>
    /// <param name="buffer">Pre-allocated array of at least <paramref name="maxLights"/> elements.</param>
    /// <param name="maxLights">Maximum number of entries to write.</param>
    /// <param name="layer">Layer filter.</param>
    /// <param name="count">Number of entries written to <paramref name="buffer"/>.</param>
    public void FillShaderBuffer(LightShaderData[] buffer, int maxLights, LightingLayer layer, out int count)
    {
        count = 0;
        for (int i = 0; i < _lights.Count && count < maxLights; i++)
        {
            var light = _lights[i];
            if (!light.IsContributing) continue;
            if (light.LightingLayer != layer) continue;

            Vector2 position = light.Entity.Transform.Position2d;
            Vector4 color = light.Color.ToVector4();
            int type;
            float innerAngle = 0f, outerAngle = 0f;
            Vector2 direction = Vector2.Zero;

            if (light is SpotLight2D spot)
            {
                type = LightShaderData.TypeSpot;
                innerAngle = spot.InnerAngle;
                outerAngle = spot.OuterAngle;
                direction = spot.Direction ?? Vector2.UnitX;
            }
            else if (light is PointLight2D)
            {
                type = LightShaderData.TypePoint;
            }
            else if (light is DirectionalLight2D dir)
            {
                type = LightShaderData.TypeDirectional;
                direction = dir.Direction;
            }
            else
            {
                type = LightShaderData.TypeAmbient;
            }

            buffer[count++] = new LightShaderData(position, light.Range, light.Intensity,
                color, type, innerAngle, outerAngle, direction);
        }
    }

    /// <summary>
    /// Sets standard light array parameters on <paramref name="effect"/> for GLSL/HLSL shaders.
    /// Parameters written: <c>_LightCount</c> (int), <c>_LightPositions</c> (Vector2[]),
    /// <c>_LightColors</c> (Vector4[]), <c>_LightRanges</c> (float[]).
    /// Missing parameters are silently ignored.
    /// Note: allocates temporary arrays — do not call every frame on a hot path.
    /// </summary>
    [Obsolete("Use FillShaderBuffer for zero-allocation GPU path.")]
    public void FillShaderParameters(Effect effect)
    {
        int count = 0;
        for (int i = 0; i < _lights.Count; i++)
            if (_lights[i].IsContributing) count++;

        effect.Parameters["_LightCount"]?.SetValue(count);

        if (count == 0) return;

        var positions = new Vector2[count];
        var colors = new Vector4[count];
        var ranges = new float[count];

        int idx = 0;
        for (int i = 0; i < _lights.Count && idx < count; i++)
        {
            var light = _lights[i];
            if (!light.IsContributing) continue;

            positions[idx] = light.Entity.Transform.Position2d;
            colors[idx] = light.Color.ToVector4() * light.Intensity;
            ranges[idx] = light.Range;
            idx++;
        }

        effect.Parameters["_LightPositions"]?.SetValue(positions);
        effect.Parameters["_LightColors"]?.SetValue(colors);
        effect.Parameters["_LightRanges"]?.SetValue(ranges);
    }
}
