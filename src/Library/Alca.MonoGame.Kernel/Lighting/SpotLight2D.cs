namespace Alca.MonoGame.Kernel.Lighting;

/// <summary>
/// Cone-shaped spot light with smooth inner/outer angle falloff and distance attenuation.
/// Requires <see cref="LightBehaviour.Range"/> > 0 to have any effect.
/// Use for flashlights, stage spotlights, and enemy cones of vision.
/// </summary>
public sealed class SpotLight2D : LightBehaviour
{
    /// <summary>Gets or sets the inner cone half-angle in degrees. Points inside this cone receive full intensity. Default is 15.</summary>
    public float InnerAngle { get; set; } = 15f;

    /// <summary>Gets or sets the outer cone half-angle in degrees. Points between inner and outer angles receive smooth falloff. Default is 30.</summary>
    public float OuterAngle { get; set; } = 30f;

    /// <summary>
    /// Gets or sets the light direction. When null, defaults to <see cref="Vector2.UnitX"/>.
    /// Typically set from <c>Entity.Transform.Rotation2d</c> each frame by the caller.
    /// </summary>
    public Vector2? Direction { get; set; }

    /// <inheritdoc/>
    public override void Contribute(ref LightContribution accumulator, Vector2 worldPosition)
    {
        if (Range <= 0f) return;

        Vector2 lightPos = Entity.Transform.Position2d;
        float dist = Vector2.Distance(lightPos, worldPosition);
        if (dist >= Range) return;

        Vector2 dir = Direction ?? Vector2.UnitX;
        if (dir == Vector2.Zero) return;

        Vector2 toPoint = worldPosition - lightPos;
        if (toPoint == Vector2.Zero)
        {
            accumulator.Add(Color, Intensity);
            return;
        }

        float cosAngle = Vector2.Dot(Vector2.Normalize(dir), Vector2.Normalize(toPoint));
        float cosInner = MathF.Cos(MathHelper.ToRadians(InnerAngle));
        float cosOuter = MathF.Cos(MathHelper.ToRadians(OuterAngle));

        if (cosAngle < cosOuter) return;

        float spotFactor = MathHelper.Clamp(
            (cosAngle - cosOuter) / (cosInner - cosOuter + float.Epsilon), 0f, 1f);
        float distFactor = 1f - (dist / Range);

        accumulator.Add(Color, Intensity * spotFactor * distFactor);
    }
}
