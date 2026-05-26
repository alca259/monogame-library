namespace Alca.MonoGame.Kernel.Mathematics;

/// <summary>Utility methods for common math operations in game contexts.</summary>
public static class MathUtils
{
    /// <summary>Returns the squared distance between two points, avoiding an expensive square root.</summary>
    public static float DistanceSquared(Vector2 a, Vector2 b) => Vector2.DistanceSquared(a, b);

    /// <summary>Returns the angle in radians from <paramref name="from"/> toward <paramref name="to"/>.</summary>
    public static float AngleBetween(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        return MathF.Atan2(delta.Y, delta.X);
    }

    /// <summary>Converts an angle in radians to a unit direction vector.</summary>
    public static Vector2 AngleToVector2(float radians) =>
        new(MathF.Cos(radians), MathF.Sin(radians));

    /// <summary>Wraps an angle in radians to the range [-π, π].</summary>
    public static float WrapAngle(float angle) => MathHelper.WrapAngle(angle);

    /// <summary>Clamps a value between a minimum and maximum.</summary>
    public static float Clamp(float value, float min, float max) =>
        MathHelper.Clamp(value, min, max);

    /// <summary>Linearly interpolates between two float values.</summary>
    public static float Lerp(float a, float b, float t) => MathHelper.Lerp(a, b, t);

    /// <summary>Linearly interpolates between two Vector2 values.</summary>
    public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => Vector2.Lerp(a, b, t);

    /// <summary>Linearly interpolates between two Color values.</summary>
    public static Color Lerp(Color a, Color b, float t) => Color.Lerp(a, b, t);

    /// <summary>Applies smooth Hermite interpolation between two float values.</summary>
    public static float SmoothStep(float a, float b, float t) => MathHelper.SmoothStep(a, b, t);

    /// <summary>Maps a value from one numeric range to another.</summary>
    public static float MapRange(float value, float inMin, float inMax, float outMin, float outMax)
    {
        float t = (value - inMin) / (inMax - inMin);
        return outMin + t * (outMax - outMin);
    }
}
