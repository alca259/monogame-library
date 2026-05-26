namespace Alca.MonoGame.Kernel.Lighting.GPU;

/// <summary>
/// GPU-friendly packed representation of a single light for shader consumption.
/// 52 bytes per instance; use <see cref="PackInto"/> to flatten into a float array
/// suitable for <c>Effect.Parameters["_LightData"].SetValue(float[])</c>.
/// </summary>
public readonly struct LightShaderData
{
    /// <summary>Number of floats produced by <see cref="PackInto"/> per light.</summary>
    public const int FloatCount = 13; // 2+1+1+4+1+1+1+2

    /// <summary>Light type constant: ambient (no position or direction).</summary>
    public const int TypeAmbient = 0;
    /// <summary>Light type constant: directional (direction only, no attenuation).</summary>
    public const int TypeDirectional = 1;
    /// <summary>Light type constant: point (position + range, radial attenuation).</summary>
    public const int TypePoint = 2;
    /// <summary>Light type constant: spot (position + direction + inner/outer cone).</summary>
    public const int TypeSpot = 3;

    /// <summary>World-space position. Unused for ambient and directional lights.</summary>
    public readonly Vector2 Position;
    /// <summary>Maximum influence radius. 0 means unlimited (ambient/directional).</summary>
    public readonly float Range;
    /// <summary>Light intensity in [0, 1].</summary>
    public readonly float Intensity;
    /// <summary>RGBA color packed as Vector4.</summary>
    public readonly Vector4 Color;
    /// <summary>Light type: 0=Ambient, 1=Directional, 2=Point, 3=Spot.</summary>
    public readonly int Type;
    /// <summary>Inner cone half-angle in degrees. Non-zero for spot lights only.</summary>
    public readonly float InnerAngle;
    /// <summary>Outer cone half-angle in degrees. Non-zero for spot lights only.</summary>
    public readonly float OuterAngle;
    /// <summary>Normalized light direction. Non-zero for directional and spot lights.</summary>
    public readonly Vector2 Direction;

    /// <summary>Initializes all fields.</summary>
    public LightShaderData(Vector2 position, float range, float intensity, Vector4 color,
        int type, float innerAngle, float outerAngle, Vector2 direction)
    {
        Position = position;
        Range = range;
        Intensity = intensity;
        Color = color;
        Type = type;
        InnerAngle = innerAngle;
        OuterAngle = outerAngle;
        Direction = direction;
    }

    /// <summary>
    /// Writes this struct's fields as 13 consecutive floats into <paramref name="buffer"/>
    /// starting at <paramref name="offset"/>. No bounds checking is performed.
    /// </summary>
    public void PackInto(float[] buffer, int offset)
    {
        buffer[offset + 0]  = Position.X;
        buffer[offset + 1]  = Position.Y;
        buffer[offset + 2]  = Range;
        buffer[offset + 3]  = Intensity;
        buffer[offset + 4]  = Color.X;
        buffer[offset + 5]  = Color.Y;
        buffer[offset + 6]  = Color.Z;
        buffer[offset + 7]  = Color.W;
        buffer[offset + 8]  = (float)Type;
        buffer[offset + 9]  = InnerAngle;
        buffer[offset + 10] = OuterAngle;
        buffer[offset + 11] = Direction.X;
        buffer[offset + 12] = Direction.Y;
    }
}
